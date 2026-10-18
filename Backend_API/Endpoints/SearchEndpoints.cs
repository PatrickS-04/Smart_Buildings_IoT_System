using Backend_API.Services;
using PROG7312_POE.Shared.DTOs;

namespace Backend_API.Endpoints
{
    public static class SearchEndpoints
    {
        public static void MapSearchEndpoints(this WebApplication application)
        {
            var group = application.MapGroup("/api/search");

            // This endpoint would be GET /api/search/devices?text=&category=&alertState=&mainZone=limit=
            group.MapGet("/devices", (
                DeviceStateService deviceStateService,
                ActivityTrackerService activityTracker,
                string? text,
                string? category,
                string? alertState,
                string? mainZone,
                int? limit,
                bool? track) =>
            {
                if (track == true)
                {
                    activityTracker.RecordSearch(text, category, alertState, mainZone);
                }

                var allDevices = deviceStateService.GetAll();

                var counts = AlertStates.AllAlertStates.ToDictionary(
                    state => state,
                    state => allDevices.Count(device => StateOf(device) == state));

                IEnumerable<LiveDeviceDto> matches = allDevices;

                if (!string.IsNullOrWhiteSpace(category))
                {
                    matches = matches.Where(device =>
                        device.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(mainZone))
                {
                    matches = matches.Where(device =>
                        device.MainZone.Equals(mainZone, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(alertState))
                {
                    matches = matches.Where(device =>
                        StateOf(device).Equals(alertState, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(text))
                {
                    var term = text.Trim();

                    matches = matches.Where(device =>
                        device.DeviceMacAddress.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        device.SensorType.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        device.SubZone.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        device.RoomOrArea.Contains(term, StringComparison.OrdinalIgnoreCase));
                }

                var ordered = matches
                    .OrderBy(device => Array.IndexOf(AlertStates.AllAlertStates, StateOf(device)))
                    .ThenBy(device => device.DeviceMacAddress)
                    .ToList();

                return Results.Ok(new DeviceSearchResultDto
                {
                    TotalMatches = ordered.Count,
                    TotalDevices = allDevices.Count,
                    CountsByAlertState = counts,
                    Devices = ordered.Take(Math.Clamp(limit ?? 200, 1, 500)).ToList()
                });
            });
        }

        private static string StateOf(LiveDeviceDto device)
        {
            if (!device.IsOnline)
            {
                return AlertStates.Offline;
            }

            return device.Severity switch
            {
                "critical" => AlertStates.Critical,
                "warn" => AlertStates.Warning,
                _ => AlertStates.Normal
            };
        }
    }
}
