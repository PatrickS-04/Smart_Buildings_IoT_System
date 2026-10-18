using PROG7312_POE.Shared;
using PROG7312_POE.Shared.DTOs;

namespace Backend_API.Services
{
    public class EpisodeDetectionService
    {
        private readonly TelemetryHistoryService _history;
        private readonly SensorRegistrationService _sensors;

        public EpisodeDetectionService(TelemetryHistoryService history, SensorRegistrationService sensors)
        {
            _history = history;
            _sensors = sensors;
        }

        public List<EpisodeDto> GetEpisodes(string deviceMacAddress)
        {
            var sensor = _sensors.GetSensorByMacAddress(deviceMacAddress);
            
            if (sensor is null)
            {
                return new List<EpisodeDto>();
            }

            var readings = _history.GetLatestReading(deviceMacAddress, 5000);

            return Detect(deviceMacAddress, sensor.SensorType, readings)
                .OrderByDescending(episode => episode.StartUtc)
                .ToList();
        }

        public List<EpisodeDto> GetAllEpisodes()
        {
            return _history.GetDeviceMacAddresses()
                .SelectMany(GetEpisodes)
                .OrderByDescending(episode => episode.StartUtc)
                .ToList();
        }

        public static List<EpisodeDto> Detect(string deviceMacAddress, string sensorType, IReadOnlyList<TelemetryReadingDto> readings)
        {
            var episodes = new List<EpisodeDto>();

            if (!SensorThresholds.Warning.TryGetValue(sensorType, out var band) || readings.Count < 3)
            {
                return episodes;
            }

            var count = readings.Count;
            var values = new double[count];

            for (var index = 0; index < count; index++)
            {
                values[index] = Convert.ToDouble(readings[index].Value);
            }

            var normalLevel = Median(values);

            string StateAt(int index)
            {
                if (band.High is { } high && values[index] > high)
                {
                    return "high";
                }

                if (band.Low is { } low && values[index] < low)
                {
                    return "low";
                }

                return "normal";
            }

            var position = 0;

            while (position < count && StateAt(position) != "normal")
            {
                position++;
            }

            while (position < count)
            {
                var state = StateAt(position);

                if (state == "normal")
                {
                    position++;
                    continue;
                }

                var start = position;
                var peakIndex = position;

                while (position < count && StateAt(position) == state)
                {
                    var isNewPeak = state == "high"
                        ? values[position] > values[peakIndex]
                        : values[position] < values[peakIndex];

                    if (isNewPeak)
                    {
                        peakIndex = position;
                    }

                    position++;
                }

                var isOngoing = position >= count;
                var last = position - 1;

                var rampStart = start;

                while (rampStart > 0 && (state == "high"
                    ? values[rampStart - 1] > normalLevel
                    : values[rampStart - 1] < normalLevel))
                {
                    rampStart--;
                }

                episodes.Add(new EpisodeDto
                {
                    DeviceMacAddress = deviceMacAddress,
                    SensorType = sensorType,
                    Unit = readings[0].Unit,
                    Kind = state,
                    Limit = state == "high" ? band.High!.Value : band.Low!.Value,
                    StartUtc = readings[start].TimeStamp,
                    EndUtc = isOngoing ? null : readings[last].TimeStamp,
                    IsOngoing = isOngoing,
                    DurationSeconds = (readings[last].TimeStamp - readings[start].TimeStamp).TotalSeconds,
                    TimeToReachSeconds = (readings[start].TimeStamp - readings[rampStart].TimeStamp).TotalSeconds,
                    PeakValue = Math.Round(values[peakIndex], 3),
                    PeakAtUtc = readings[peakIndex].TimeStamp
                });
            }

            return episodes;
        }

        private static double Median(double[] values)
        {
            var sorted = values.OrderBy(value => value).ToArray();
            var middle = sorted.Length / 2;

            return sorted.Length % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
        }
    }
}
