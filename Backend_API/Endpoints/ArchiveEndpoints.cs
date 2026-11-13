using Backend_API.Services;
using PROG7312_POE.Shared.DTOs;
using PROG7312_POE.Shared.Models;
using System.Diagnostics;

namespace Backend_API.Endpoints
{
    public static class ArchiveEndpoints
    {
        public static void MapArchiveEndpoints(this WebApplication application)
        {
            var group = application.MapGroup("/api/archive");

            // GET /api/archive/{mac}?from=2026-11-11T10:00:00Z&to=2026-11-11T10:10:00Z&limit=500
            // Use a "Z" or an offset on the dates, otherwise the server assumes its own local time
            group.MapGet("/{deviceMacAddress}", (string deviceMacAddress, DateTimeOffset? from, DateTimeOffset? to, int? limit,
                SensorRegistrationService sensorRegistrationService, TelemetryHistoryService telemetryHistoryService) =>
            {
                if (sensorRegistrationService.GetSensorByMacAddress(deviceMacAddress) is null)
                {
                    return Results.NotFound(new { error = $"No registered device with MAC address {deviceMacAddress}" });
                }

                var toTimeStamp = to?.UtcDateTime ?? DateTime.UtcNow;
                var fromTimeStamp = from?.UtcDateTime ?? toTimeStamp.AddHours(-1);

                if (fromTimeStamp > toTimeStamp)
                {
                    return Results.BadRequest(new { error = "'from' must not be later than 'to'" });
                }

                var matches = telemetryHistoryService.QueryArchive(deviceMacAddress, fromTimeStamp, toTimeStamp, out var nodesVisited);
                var stats = telemetryHistoryService.GetArchiveStats(deviceMacAddress);
                var take = Math.Clamp(limit ?? 500, 1, 5000);

                return Results.Ok(new ArchiveQueryResultDto
                {
                    DeviceMacAddress = deviceMacAddress,
                    FromTimeStamp = fromTimeStamp,
                    ToTimeStamp = toTimeStamp,
                    TotalStored = stats.ReadingCount,
                    MatchCount = matches.Count,
                    ReturnedCount = Math.Min(take, matches.Count),
                    NodesVisited = nodesVisited,
                    TelemetryReadings = matches.Take(take).ToList()
                });
            });

            group.MapGet("/{deviceMacAddress}/stats", (string deviceMacAddress, TelemetryHistoryService telemetryHistoryService) =>
            {
                return Results.Ok(telemetryHistoryService.GetArchiveStats(deviceMacAddress));
            });

            // Demo helper: loads a large amount of mock history (before the oldest reading the sensor already has),
            // so the tree can be shown working under load. Numeric sensors only
            group.MapPost("/test/seed", (string deviceMacAddress, int? count, SensorRegistrationService sensorRegistrationService,
                TelemetryHistoryService telemetryHistoryService) =>
            {
                var sensor = sensorRegistrationService.GetSensorByMacAddress(deviceMacAddress);

                if (sensor is null)
                {
                    return Results.NotFound(new { error = $"No registered device with MAC address {deviceMacAddress}" });
                }

                if (!ProfileOfSensorSignal.IsCyclic(sensor.SensorType))
                {
                    return Results.BadRequest(new { error = $"{sensor.SensorType} is not a numeric sensor" });
                }

                var total = Math.Clamp(count ?? 100_000, 1_000, 500_000);
                var existing = telemetryHistoryService.GetArchiveStats(deviceMacAddress);
                var end = existing.OldestTimeStamp ?? DateTime.UtcNow;
                var start = end.AddSeconds(-10.0 * total);
                var unit = ProfileOfSensorSignal.UnitOf(sensor.SensorType);
                var random = new Random();

                var stopwatch = Stopwatch.StartNew();

                for (var index = 0; index < total; index++)
                {
                    var moment = start.AddSeconds(10.0 * index);
                    var value = ProfileOfSensorSignal.ValueAt(sensor.SensorType, deviceMacAddress, moment, random);

                    if (sensor.SensorType == "People Counting")
                    {
                        telemetryHistoryService.Add(new TelemetryPacketModel<int>(deviceMacAddress, sensor.SensorType, (int)Math.Round(value), unit, moment));
                    }
                    else
                    {
                        telemetryHistoryService.Add(new TelemetryPacketModel<float>(deviceMacAddress, sensor.SensorType, (float)Math.Round(value, 3), unit, moment));
                    }
                }

                stopwatch.Stop();

                return Results.Ok(new
                {
                    seeded = total,
                    from = start,
                    to = end,
                    seedMilliseconds = stopwatch.ElapsedMilliseconds,
                    archive = telemetryHistoryService.GetArchiveStats(deviceMacAddress)
                });
            });

            // Compares the AVL tree range query with a plain scan over every reading
            // Without from/to it uses a 10-minute window in the middle of the stored history
            group.MapGet("/{deviceMacAddress}/benchmark", (string deviceMacAddress, DateTimeOffset? from, DateTimeOffset? to,
                int? repeats, TelemetryHistoryService telemetryHistoryService) =>
            {
                var all = telemetryHistoryService.GetArchivedReadings(deviceMacAddress);

                if (all.Count == 0)
                {
                    return Results.BadRequest(new { error = "This device has no archived readings yet" });
                }

                var oldest = all[0].TimeStamp;
                var newest = all[^1].TimeStamp;
                var middle = oldest.AddTicks((newest - oldest).Ticks / 2);

                var fromTimeStamp = from?.UtcDateTime ?? middle.AddMinutes(-5);
                var toTimeStamp = to?.UtcDateTime ?? middle.AddMinutes(5);
                var runs = Math.Clamp(repeats ?? 200, 10, 5000);

                telemetryHistoryService.QueryArchive(deviceMacAddress, fromTimeStamp, toTimeStamp, out _);
                all.Where(reading => reading.TimeStamp >= fromTimeStamp && reading.TimeStamp <= toTimeStamp).ToList();

                var treeMatches = 0;
                var nodesVisited = 0;
                var treeWatch = Stopwatch.StartNew();

                for (var run = 0; run < runs; run++)
                {
                    treeMatches = telemetryHistoryService.QueryArchive(deviceMacAddress, fromTimeStamp, toTimeStamp, out nodesVisited).Count;
                }

                treeWatch.Stop();

                var scanMatches = 0;
                var scanWatch = Stopwatch.StartNew();

                for (var run = 0; run < runs; run++)
                {
                    scanMatches = all.Where(reading => reading.TimeStamp >= fromTimeStamp && reading.TimeStamp <= toTimeStamp).ToList().Count;
                }

                scanWatch.Stop();

                var treeMs = treeWatch.Elapsed.TotalMilliseconds / runs;
                var scanMs = scanWatch.Elapsed.TotalMilliseconds / runs;

                return Results.Ok(new
                {
                    readingsStored = all.Count,
                    windowFromTimeStamp = fromTimeStamp,
                    windowToTimeStamp = toTimeStamp,
                    matches = treeMatches,
                    scanMatches,
                    runs,
                    treeMillisecondsPerQuery = Math.Round(treeMs, 4),
                    scanMillisecondsPerQuery = Math.Round(scanMs, 4),
                    speedup = treeMs > 0 ? Math.Round(scanMs / treeMs, 1) : 0,
                    nodesVisitedByTree = nodesVisited,
                    nodesExaminedByScan = all.Count
                });
            });
        }
    }
}
