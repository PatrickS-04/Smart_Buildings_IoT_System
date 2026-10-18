using Backend_API.Services;
using PROG7312_POE.Shared.Models;

namespace Backend_API.Endpoints
{
    public static class AnalyticsEndpoints
    {
        public static void MapAnalyticsEndpoints(this WebApplication application)
        {
            var group = application.MapGroup("/api/analytics");

            group.MapGet("/episodes/{deviceMacAddress}", (string deviceMacAddress,
                EpisodeDetectionService episodeDetector, SensorRegistrationService sensorService) =>
            {
                if (sensorService.GetSensorByMacAddress(deviceMacAddress) is null)
                {
                    return Results.NotFound(new { error = $"No registered device with MAC address {deviceMacAddress}" });
                }

                return Results.Ok(episodeDetector.GetEpisodes(deviceMacAddress));
            });

            group.MapGet("/episodes", (EpisodeDetectionService episodeDetector, int? take) =>
            {
                return Results.Ok(episodeDetector.GetAllEpisodes().Take(Math.Clamp(take ?? 30, 1, 200)));
            });

            group.MapPost("/test/backfill", (SensorRegistrationService sensorRegistrationService, TelemetryHistoryService historyService, int? minutes) =>
            {
                var windowMinutes = Math.Clamp(minutes ?? 60, 10, 240);
                var now = DateTime.UtcNow;
                var from = now.AddMinutes(-windowMinutes);
                var random = new Random();

                var filled = 0;
                var skipped = 0;

                foreach (var sensor in sensorRegistrationService.GetAllSensors())
                {
                    if (!ProfileOfSensorSignal.IsCyclic(sensor.SensorType))
                    {
                        continue;
                    }

                    if (historyService.GetBetween(sensor.DeviceMacAddress, from, from.AddMinutes(1)).Count > 0)
                    {
                        skipped++;
                        continue;
                    }

                    var unit = ProfileOfSensorSignal.UnitOf(sensor.SensorType);

                    for (var moment = from; moment < now.AddSeconds(-10); moment = moment.AddSeconds(10))
                    {
                        var value = ProfileOfSensorSignal.ValueAt(sensor.SensorType, sensor.DeviceMacAddress, moment, random);


                        if (sensor.SensorType == "People Counting")
                        {
                            historyService.Add(new TelemetryPacketModel<int>(
                                sensor.DeviceMacAddress, sensor.SensorType, (int)Math.Round(value), unit, moment));
                        }
                        else
                        {
                            historyService.Add(new TelemetryPacketModel<float>(
                                sensor.DeviceMacAddress, sensor.SensorType, (float)Math.Round(value, 3), unit, moment));
                        }
                    }

                    filled++;
                }

                return Results.Ok(new { windowMinutes, sensorsFilled = filled, sensorsSkipped = skipped });
            });
        }
    }
}
