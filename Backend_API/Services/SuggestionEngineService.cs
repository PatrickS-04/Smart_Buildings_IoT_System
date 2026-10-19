using PROG7312_POE.Shared;
using PROG7312_POE.Shared.DTOs;
using System.Diagnostics;

namespace Backend_API.Services
{
    public class SuggestionEngineService
    {
        private const int MinRepeatsForView = 3;
        private const int MinRepeatsForResponse = 3;
        private const int MinRepeatsForHabit = 5;
        private const double ImminentSeconds = 120;
        private const double InsightLookAheadSeconds = 180;

        private readonly ActivityTrackerService _activityTracker;
        private readonly DeviceStateService _devices;
        private readonly AlertStateService _alerts;
        private readonly PredictionEngineService _predictionEngine;
        private readonly SensorRegistrationService _sensors;
        private readonly CommandService _commands;

        public SuggestionEngineService(ActivityTrackerService activityTracker, DeviceStateService devices, AlertStateService alerts,
            PredictionEngineService predictionEngine, SensorRegistrationService sensors, CommandService commands)
        {
            _activityTracker = activityTracker;
            _devices = devices;
            _alerts = alerts;
            _predictionEngine = predictionEngine;
            _sensors = sensors;
            _commands = commands;
        }

        public SuggestionsDto Build()
        {
            var now = DateTime.UtcNow;
            var searches = _activityTracker.GetSearches();
            var commands = _activityTracker.GetCommands();
            var liveDevices = _devices.GetAll();
            var forecasts = _predictionEngine.GetAllForecasts();

            return new SuggestionsDto
            {
                TrackedSearches = searches.Count,
                TrackedCommands = commands.Count,
                Insights = BuildInsights(searches, commands, liveDevices, forecasts, now),
                SuggestedActions = BuildActions(searches, commands, liveDevices, forecasts, now)
            };
        }

        private List<SuggestedActionDto> BuildActions(List<SearchActivity> searches, List<CommandActivity> commands,
            List<LiveDeviceDto> liveDevices, List<SensorForecastDto> forecasts, DateTime now)
        {
            var actions = new List<SuggestedActionDto>();

            var usualViews = searches
                .GroupBy(search => $"{search.Text.ToLowerInvariant()}|{search.Category}|{search.AlertState}|{search.Zone}")
                .Where(group => group.Count() >= MinRepeatsForView)
                .OrderByDescending(group => group.Count())
                .ThenByDescending(group => group.Last().AtUtc)
                .Take(3);

            foreach (var group in usualViews)
            {
                var latest = group.Last();

                actions.Add(new SuggestedActionDto
                {
                    Id = $"view:{group.Key}",
                    Kind = "view",
                    Reason = "view",
                    Title = $"Open your usual view: {DescribeView(latest)}",
                    Detail = $"You have used this search {group.Count()} times",
                    Score = 20 + group.Count(),
                    EvidenceCount = group.Count(),
                    ActionType = "applyFilter",
                    FilterText = latest.Text,
                    FilterCategory = latest.Category,
                    FilterAlertState = latest.AlertState,
                    FilterZone = latest.Zone
                });
            }

            var actuators = _sensors.GetAllSensors()
                .Where(sensor => sensor.Category == SensorCategorization.ActuatorCategory)
                .ToList();

            var activeTriggers = ActiveTriggerTypes(liveDevices);
            var expectedTriggers = ExpectedTriggerTypes(forecasts, now);
            var covered = new HashSet<string>();

            var triggerTotals = commands.SelectMany(command => command.TriggeredSensorTypes.Distinct())
                    .GroupBy(trigger => trigger)
                    .ToDictionary(group => group.Key, group => group.Count());

            var responses = commands
                .SelectMany(command => command.TriggeredSensorTypes.Distinct()
                    .Select(trigger => (Trigger: trigger, command.SensorType, command.MainZone, command.TurnOn)))
                .GroupBy(item => item)
                .Where(group => group.Count() >= MinRepeatsForResponse)
                .OrderByDescending(group => group.Count());

            foreach (var group in responses)
            {
                var (trigger, actuatorType, zone, turnOn) = group.Key;

                var isActive = activeTriggers.Contains(trigger);
                var isExpected = expectedTriggers.TryGetValue(trigger, out var secondsAway);

                if (!isActive && !isExpected)
                {
                    continue;
                }

                var targets = TargetsThatWouldChange(actuators, actuatorType, zone, turnOn);
                var commandKey = $"{actuatorType}|{zone}|{turnOn}";

                if (targets == 0 || !covered.Add(commandKey))
                {
                    continue;
                }

                var share = (int)Math.Round(100.0 * group.Count() / triggerTotals[trigger]);

                var detail = isActive
                    ? $"{trigger} is alerting right now. You responded this way {group.Count()} of {triggerTotals[trigger]} times when it alerted."
                    : $"{trigger} is predicted to go out of range in about {Duration(secondsAway)}. You responded this way {group.Count()} of {triggerTotals[trigger]} times when it alerted.";

                actions.Add(CommandAction(
                    id: $"response:{trigger}|{commandKey}",
                    reason: isActive ? "Active" : "predicted",
                    actuatorType, zone, turnOn, targets, detail,
                    score: 60 + share / 2 + (isActive ? 30 : 0) + Math.Min(group.Count(), 10),
                    evidence: group.Count()));
            }

            var habits = commands
                .GroupBy(command => (command.SensorType, command.MainZone, command.TurnOn))
                .Where(group => group.Count() >= MinRepeatsForHabit)
                .OrderByDescending(group => group.Count());

            foreach (var group in habits)
            {
                var (actuatorType, zone, turnOn) = group.Key;
                var commandKey = $"{actuatorType}|{zone}|{turnOn}";
                var targets = TargetsThatWouldChange(actuators, actuatorType, zone, turnOn);

                if (targets == 0 || !covered.Add(commandKey))
                {
                    continue;
                }

                actions.Add(CommandAction(
                    id: $"habit:{commandKey}",
                    reason: "habit",
                    actuatorType, zone, turnOn, targets,
                    detail: $"You have sent this command {group.Count()} times.",
                    score: 30 + group.Count(),
                    evidence: group.Count()));
            }

            return actions.OrderByDescending(action => action.Score).Take(6).ToList();
        }

        private static SuggestedActionDto CommandAction(string id, string reason, string actuatorType, string zone, bool turnOn,
            int targets, string detail, int score, int evidence)
        {
            var onOff = turnOn ? "ON" : "OFF";
            var where = string.IsNullOrEmpty(zone) ? string.Empty : $" in {zone}";

            var title = targets == 1
                ? $"Turn {actuatorType} {onOff}{where}"
                : $"Turn all {actuatorType} devices {onOff}{where} ({targets} devices)";

            return new SuggestedActionDto
            {
                Id = id,
                Kind = "command",
                Reason = reason,
                Title = title,
                Detail = detail,
                Score = score,
                EvidenceCount = evidence,
                ActionType = "bulkCommand",
                SensorType = actuatorType,
                MainZone = zone,
                TurnOn = turnOn
            };
        }

        private int TargetsThatWouldChange(List<SensorRegistrationDto> actuators, string actuatorType, string zone, bool turnOn)
        {
            return actuators.Count(actuator =>
                actuator.SensorType == actuatorType
                && (string.IsNullOrEmpty(zone) || actuator.MainZone == zone)
                && _commands.GetStateOfCommand(actuator.DeviceMacAddress) != turnOn);
        }

        private HashSet<string> ActiveTriggerTypes(List<LiveDeviceDto> liveDevices)
        {
            var byMacAddress = liveDevices.ToDictionary(device => device.DeviceMacAddress);
            var types = new HashSet<string>();

            foreach (var key in _alerts.GetSnapshot().ActiveAlertKeys)
            {
                var parts = key.Split('|');

                if (parts.Length == 2 && parts[1] != "OFFLINE" && byMacAddress.TryGetValue(parts[0], out var device))
                {
                    types.Add(device.SensorType);
                }
            }

            return types;
        }

        private static Dictionary<string, double> ExpectedTriggerTypes(List<SensorForecastDto> forecasts, DateTime now)
        {
            var expected = new Dictionary<string, double>();

            foreach (var forecast in forecasts)
            {
                foreach (var prediction in forecast.Predictions.Where(prediction => !prediction.InProgress))
                {
                    var seconds = (prediction.PredictedStart - now).TotalSeconds;

                    if ((seconds is > 0 and <= ImminentSeconds &&
                        (!expected.TryGetValue(forecast.SensorType, out var existing) || seconds < existing)))
                    {
                        expected[forecast.SensorType] = seconds;
                    }
                }
            }

            return expected;
        }

        private List<InsightDto> BuildInsights(List<SearchActivity> searches, List<CommandActivity> commands,
            List<LiveDeviceDto> liveDevices, List<SensorForecastDto> forecasts, DateTime now)
        {
            var insights = new List<InsightDto>();

            var critical = liveDevices.Count(device => device.IsOnline && device.Severity == "critical");
            var warning = liveDevices.Count(device => device.IsOnline && device.Severity == "warn");
            var offline = liveDevices.Count(device => !device.IsOnline);

            if (liveDevices.Count == 0)
            {
                insights.Add(new InsightDto { Severity = "info", Message = "No devices are reporting yet. Register sensors and they will appear here." });
            }
            else if (critical + warning + offline == 0)
            {
                insights.Add(new InsightDto { Severity = "info", Message = $"All {liveDevices.Count} devices are reporting normally." });
            }
            else
            {
                var worstZone = liveDevices
                    .Where(device => !device.IsOnline || device.Severity != string.Empty)
                    .GroupBy(device => device.MainZone)
                    .OrderByDescending(group => group.Count())
                    .First();

                insights.Add(new InsightDto
                {
                    Severity = critical > 0 ? "critical" : "warn",
                    Message = $"{critical + warning + offline} of {liveDevices.Count} devices need attention: {critical} critical, {offline} offline, {warning} warning. " +
                        $"Most affected zone: {worstZone.Key} ({worstZone.Count()})."
                });
            }

            var alertCounts = _alerts.GetSnapshot().RecentEvents
                .Where(alertEvent => !alertEvent.IsResolution && alertEvent.Kind != "OFFLINE")
                .GroupBy(alertEvent => alertEvent.SensorType)
                .OrderByDescending(group => group.Count())
                .ToList();

            if (alertCounts.Count > 0 && alertCounts[0].Count() >= 2 &&
                (alertCounts.Count == 1 || alertCounts[0].Count() > alertCounts[1].Count()))
            {
                insights.Add(new InsightDto
                {
                    Severity = "warn",
                    Message = $"{alertCounts[0].Key} has raised {alertCounts[0].Count()} alerts recently, more than any other sensor type."
                });
            }

            var comingUp = forecasts
                .SelectMany(forecast => forecast.Predictions
                    .Where(prediction => !prediction.InProgress)
                    .Select(prediction => (Forecast: forecast, Prediction: prediction, Seconds: (prediction.PredictedStart - now).TotalSeconds)))
                .Where(item => item.Seconds is > 0 and <= InsightLookAheadSeconds)
                .OrderBy(item => item.Seconds)
                .Take(3);

            foreach (var item in comingUp)
            {
                insights.Add(new InsightDto
                {
                    Severity = "warn",
                    Message = $"{item.Forecast.SensorType} ({item.Forecast.Location}) is expected to go {item.Prediction.Kind} in about " +
                        $"{Duration(item.Seconds)} (confidence {item.Prediction.ConfidencePercentage}%)."
                });
            }

            var mostUsed = commands
                .GroupBy(command => (command.SensorType, command.TurnOn))
                .OrderByDescending(group => group.Count())
                .FirstOrDefault();

            if (mostUsed is not null && mostUsed.Count() >= 3)
            {
                insights.Add(new InsightDto
                {
                    Severity = "info",
                    Message = $"Your most frequent command is turning {mostUsed.Key.SensorType} devices {(mostUsed.Key.TurnOn ? "ON" : "OFF")} ({mostUsed.Count()} times)."
                });
            }

            if (searches.Count < 2 && commands.Count < 2)
            {
                insights.Add(new InsightDto
                {
                    Severity = "info",
                    Message = "Learning your habits. Suggestions appear once a search or command has been repeated."
                });
            }

            return insights;
        }

        private static string DescribeView(SearchActivity search)
        {
            var parts = new List<string>();

            if (search.AlertState.Length > 0)
            {
                parts.Add(char.ToUpper(search.AlertState[0]) + search.AlertState[1..]);
            }

            if (search.Category.Length > 0)
            {
                parts.Add(search.Category);
            }

            if (search.Zone.Length > 0)
            {
                parts.Add(search.Zone);
            }

            if (search.Text.Length > 0)
            {
                parts.Add($"\"{search.Text}\"");
            }

            return string.Join(" - ", parts);
        }

        private static string Duration(double seconds)
        {
            if (seconds < 60)
            {
                return $"{seconds:0}s";
            }

            if (seconds < 3600)
            {
                return $":{ (int)(seconds / 60)}m {(int)(seconds % 60):00}s";
            }

            return $"{(int)(seconds / 3600)}h {(int)(seconds % 3600 / 60)}m";
        }
    }
}
