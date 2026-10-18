using PROG7312_POE.Shared.DTOs;
using PROG7312_POE.Shared.Models;

namespace Backend_API.Services
{
    public class TelemetryHistoryService
    {
        private const int MaxReadingsPerDevice = 2000;

        private readonly Dictionary<string, SortedDictionary<DateTime, TelemetryReadingDto>> _telemetryHistory = new();
        private readonly object _lock = new();

        public void Add(ITelemetryPacketModel telemetryPacket)
        {
            lock (_lock)
            {
                if (!_telemetryHistory.TryGetValue(telemetryPacket.DeviceMacAddress, out var timeline))
                {
                    timeline = new SortedDictionary<DateTime, TelemetryReadingDto>();
                    _telemetryHistory[telemetryPacket.DeviceMacAddress] = timeline;
                }

                var key = telemetryPacket.TimeStamp;
                
                while (timeline.ContainsKey(key))
                {
                    key = key.AddTicks(1);
                }

                timeline[key] = TelemetryReadingDto.FromPacket(telemetryPacket);

                if (timeline.Count > MaxReadingsPerDevice)
                {
                    timeline.Remove(timeline.Keys.First());
                }
            }
        }

        public List<TelemetryReadingDto> GetLatestReading(string deviceMacAddress, int count)
        {
            lock (_lock)
            {
                if (!_telemetryHistory.TryGetValue(deviceMacAddress, out var timeline))
                {
                    return new List<TelemetryReadingDto>();
                }

                var skip = Math.Max(0, timeline.Count - count);
                return timeline.Values.Skip(skip).ToList();
            }
        }

        public List<TelemetryReadingDto> GetBetween(string deviceMacAddress, DateTime fromUtc, DateTime toUtc)
        {
            lock (_lock)
            {
                if (!_telemetryHistory.TryGetValue(deviceMacAddress, out var timeline))
                {
                    return new List<TelemetryReadingDto>();
                }

                return timeline
                    .Where(pair => pair.Key >= fromUtc && pair.Key <= toUtc)
                    .Select(pair => pair.Value)
                    .ToList();
            }
        }

        public List<string> GetDeviceMacAddresses()
        {
            lock (_lock)
            {
                return _telemetryHistory.Keys.ToList();
            }
        }
    }
}
