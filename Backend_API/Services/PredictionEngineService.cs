using PROG7312_POE.Shared;
using PROG7312_POE.Shared.DTOs;

namespace Backend_API.Services
{
    public class PredictionEngineService
    {
        private const int MaxEpisodesUsed = 15;
        private const int MinEpisodesNeeded = 5;
        private const int TrendWindow = 10;
        private const double TrendLookAheadInSseconds = 30 * 60;

        private readonly TelemetryHistoryService _telemetryHistory;
        private readonly SensorRegistrationService _sensors;

        public PredictionEngineService(TelemetryHistoryService telemetryHistory, SensorRegistrationService sensors)
        {
            _telemetryHistory = telemetryHistory;
            _sensors = sensors;
        }

        public SensorForecastDto? GetSensorForecast(string deviceMacAddress)
        {
            var sensor = _sensors.GetSensorByMacAddress(deviceMacAddress);

            if (sensor is null)
            {
                return null;
            }

            var readings = _telemetryHistory.GetLatestReading(deviceMacAddress, 5000);
            return Forecast(sensor.DeviceMacAddress, sensor.SensorType, SensorLocation.Format(sensor), 
                sensor.MainZone, readings, DateTime.UtcNow);
        }

        public List<SensorForecastDto> GetAllForecasts()
        {
            var forecasts = new List<SensorForecastDto>();

            foreach (var deviceMacAddress in _telemetryHistory.GetDeviceMacAddresses())
            {
                var forecast = GetSensorForecast(deviceMacAddress);

                if (forecast is not null)
                {
                    forecasts.Add(forecast);
                }
            }

            return forecasts.OrderBy(forecast => forecast.NextEvent ?? DateTime.MaxValue).ToList();
        }

        private static SensorForecastDto? Forecast(string deviceMacAddress, string sensorType, string location, string mainZone,
            List<TelemetryReadingDto> readings, DateTime now)
        {
            if (!SensorThresholds.Warning.TryGetValue(sensorType, out var band) || readings.Count == 0)
            {
                return null;
            }

            var unit = readings[^1].Unit;
            var current = Convert.ToDouble(readings[^1].Value);
            var (slopePerSecond, trend) = FitTrend(readings);

            var forecast = new SensorForecastDto
            {
                DeviceMacAddress = deviceMacAddress,
                SensorType = sensorType,
                Unit = unit,
                Location = location,
                MainZone = mainZone,
                CurrentValue = Math.Round(current, 3),
                Trend = trend,
                SlopePerMinute = Math.Round(slopePerSecond * 60, 4)
            };

            var episodes = EpisodeDetectionService.Detect(deviceMacAddress, sensorType, readings);

            foreach (var kind in new[] { "high", "low" })
            {
                var ofThisKind = episodes.Where(episode => episode.Kind == kind).OrderBy(episode => episode.StartUtc).ToList();
                var prediction = PredictKind(kind, ofThisKind, now);

                if (prediction is not null)
                {
                    forecast.Predictions.Add(prediction);
                }
            }

            forecast.TrendWarning = FindTrendWarning(band, current, slopePerSecond);
            forecast.HasEnoughData = forecast.Predictions.Count > 0;

            forecast.NextEvent = forecast.Predictions.Select(
                prediction => (DateTime?)(prediction.InProgress ? now : prediction.PredictedStart)).Min();

            forecast.Insight = BuildInsight(forecast, now);
            return forecast;
        }

        private static PredictionDto? PredictKind(string kind, List<EpisodeDto> episodesOfKind, DateTime now)
        {
            var completed = episodesOfKind.Where(episode => !episode.IsOngoing).TakeLast(MaxEpisodesUsed).ToList();

            if (completed.Count < MinEpisodesNeeded)
            {
                return null;
            }

            var gaps = new List<double>();

            for (var index = 1; index < completed.Count; index++)
            {
                gaps.Add((completed[index].StartUtc - completed[index - 1].StartUtc).TotalSeconds);
            }

            var cycle = gaps.Average();

            if (cycle <= 0)
            {
                return null;
            }

            var spread = Math.Sqrt(gaps.Average(gap => (gap - cycle) * (gap - cycle)));
            var variation = spread / cycle;

            var duration = completed.Average(episode => episode.DurationSeconds);
            var timeToReach = completed.Average(episode => episode.TimeToReachSeconds);
            var peak = completed.Average(episode => episode.PeakValue);

            var latest = episodesOfKind[^1];
            var inProgress = latest.IsOngoing;
            var nextStart = latest.StartUtc.AddSeconds(cycle);

            var guard = 0;

            while (nextStart < now && guard++ < 10000)
            {
                nextStart = nextStart.AddSeconds(cycle);
            }

            var regularity = Math.Clamp(1 - variation * 2.5, 0, 1);
            var amountOfData = Math.Min(1.0, completed.Count / 6.0);
            var confidence = (int)Math.Round(regularity * amountOfData * 100);

            return new PredictionDto
            {
                Kind = kind,
                Limit = latest.Limit,
                InProgress = inProgress,
                CrrentExpectedEnd = inProgress ? latest.StartUtc.AddSeconds(duration) : null,
                PredictedStart = nextStart,
                PredictedEnd = nextStart.AddSeconds(duration),
                CycleSeconds = Math.Round(cycle, 1),
                ExpectedDurationInSeconds = Math.Round(duration, 1),
                ExpectedTimeToReachInSeconds = Math.Round(timeToReach, 1),
                ExpectedPeak = Math.Round(peak, 3),
                AnalysedEpisode = completed.Count,
                ConfidencePercentage = confidence,
                ConfidenceLabel = confidence >= 75 ? "High" : confidence >= 45 ? "Medium" : "Low"
            };
        }

        private static (double SlopePerSecond, string Trend) FitTrend(List<TelemetryReadingDto> readings)
        {
            var window = readings.TakeLast(TrendWindow).ToList();

            if (window.Count < 3)
            {
                return (0, "steady");
            }

            var origin = window[0].TimeStamp;
            var xs = window.Select(reading => (reading.TimeStamp - origin).TotalSeconds).ToArray();
            var ys = window.Select(reading => Convert.ToDouble(reading.Value)).ToArray();

            var meanX = xs.Average();
            var meanY = ys.Average();

            var denominator = xs.Sum(x => (x - meanX) * (x - meanX));

            if (denominator == 0)
            {
                return (0, "steady");
            }

            var slope = xs.Zip(ys, (x, y) => (x - meanX) * (y - meanY)).Sum() / denominator;

            var steadyLimit = 0.005 * Math.Max(Math.Abs(meanY), 0.01);

            if (Math.Abs(slope * 60) < steadyLimit)
            {
                return (slope, "steady");
            }

            return (slope, slope > 0 ? "rising" : "falling");
        }

        private static TrendWarningDto? FindTrendWarning(ThresholdBand band, double current, double slopePerSecond)
        {
            double secondsUntilLimitCrossed;
            string kind;
            double limit;

            if (slopePerSecond > 0 && band.High is { } high && current <= high)
            {
                secondsUntilLimitCrossed = (high - current) / slopePerSecond;
                kind = "high";
                limit = high;
            }
            else if (slopePerSecond < 0 && band.Low is { } low && current >= low)
            {
                secondsUntilLimitCrossed = (current - low) / -slopePerSecond;
                kind = "low";
                limit = low;
            }
            else
            {
                return null;
            }

            if (secondsUntilLimitCrossed > TrendLookAheadInSseconds)
            {
                return null;
            }

            return new TrendWarningDto { Kind = kind, Limit = limit, SecondsUntilLimitCrossed = Math.Round(secondsUntilLimitCrossed, 1) };
        }

        private static string BuildInsight(SensorForecastDto forecast, DateTime now)
        {
            if (forecast.Predictions.Count == 0)
            {
                return $"Still learning {forecast.SensorType} ({forecast.Location}). At least 5 completed highs or lows are needed before a pattern can be forecasted.";
            }

            var sentences = new List<string>();

            foreach (var prediction in forecast.Predictions)
            {
                var direction = prediction.Kind == "high" ? "climb" : "drop";

                var timing = prediction.InProgress
                    ? $"One is happening now and should end in about {Duration(Math.Max(0, (prediction.CrrentExpectedEnd!.Value - now).TotalSeconds))}; the next is expected in about {Duration(Math.Max(0, (prediction.PredictedStart - now).TotalSeconds))}."
                    : $"The next one is expected in about {Duration(Math.Max(0, (prediction.PredictedStart - now).TotalSeconds))}.";

                sentences.Add(
                    $"{forecast.SensorType} usually goes {prediction.Kind} about every {Duration(prediction.CycleSeconds)}, " +
                    $"taking {Duration(prediction.ExpectedTimeToReachInSeconds)} to {direction} from its normal level to the limit " +
                    $"and staying {prediction.Kind} for about {Duration(prediction.ExpectedDurationInSeconds)}, " +
                    $"peaking near {prediction.ExpectedPeak:0.##} {forecast.Unit}. {timing} Confidence: {prediction.ConfidencePercentage}% ({prediction.ConfidenceLabel}).");
            }

            return string.Join(" ", sentences);
        }

        private static string Duration(double seconds)
        {
            if (seconds < 60)
            {
                return $"{seconds:0}s";
            }

            if (seconds < 3600)
            {
                return $"{(int)(seconds / 60)}m {(int)(seconds % 60):00}s";
            }

            return $"{(int)(seconds / 3600)}h {(int)(seconds % 3600 / 60)}m";
        }
    }
}
