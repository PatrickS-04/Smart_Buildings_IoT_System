using Backend_API.Data_Structures;
using PROG7312_POE.Shared.DTOs;
using PROG7312_POE.Shared.Models;

namespace Backend_API.Services
{

    // Two views of the same readings:
    //  - SortedDictionary: only the newest 2000 per device, for quick "latest N" lookups (Part 2).
    //  - AVL tree: the full archive, for fast time-range queries (Part 3)
    public class TelemetryHistoryService
    {
        private const int MaxReadingsPerDevice = 2000;

        private readonly Dictionary<string, SortedDictionary<DateTime, TelemetryReadingDto>> _telemetryHistory = new();
        private readonly Dictionary<string, AvlTree<DateTime, TelemetryReadingDto>> _archive = new();
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

                if (!_archive.TryGetValue(telemetryPacket.DeviceMacAddress, out var avlTree))
                {
                    avlTree = new AvlTree<DateTime, TelemetryReadingDto>();
                    _archive[telemetryPacket.DeviceMacAddress] = avlTree;
                }

                var key = telemetryPacket.TimeStamp;
                
                while (timeline.ContainsKey(key))
                {
                    key = key.AddTicks(1);
                }

                var reading = TelemetryReadingDto.FromPacket(telemetryPacket);

                timeline[key] = reading;
                avlTree.Insert(key, reading);

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

        public List<TelemetryReadingDto> GetBetween(string deviceMacAddress, DateTime fromTimeStamp, DateTime toTimeStamp)
        {
            return QueryArchive(deviceMacAddress, fromTimeStamp, toTimeStamp, out _);
        }

        public List<TelemetryReadingDto> QueryArchive(string deviceMacAddress, DateTime fromTimeStamp, DateTime toTimeStamp, out int nodesVisited)
        {
            lock (_lock)
            {
                if (!_archive.TryGetValue(deviceMacAddress, out var avlTree))
                {
                    nodesVisited = 0;
                    return new List<TelemetryReadingDto>();
                }

                return avlTree.Range(fromTimeStamp, toTimeStamp, out nodesVisited);
            }
        }

        public List<TelemetryReadingDto> GetArchivedReadings(string deviceMacAddress)
        {
            lock (_lock)
            {
                return _archive.TryGetValue(deviceMacAddress, out var avlTree)
                    ? avlTree.InOrder()
                    : new List<TelemetryReadingDto>();
            }
        }

        public ArchiveStatsDto GetArchiveStats(string deviceMacAddress)
        {
            lock (_lock)
            {
                var stats = new ArchiveStatsDto { DeviceMacAddress = deviceMacAddress };

                if (!_archive.TryGetValue(deviceMacAddress, out var avlTree) || avlTree.Count == 0)
                {
                    return stats;
                }

                stats.ReadingCount = avlTree.Count;
                stats.TreeHeight = avlTree.Height;
                stats.MinimumPossibleHeight = (int)Math.Ceiling(Math.Log2(avlTree.Count + 1));
                stats.RotationCount = avlTree.Rotations;

                if (avlTree.TryGetMaxKey(out var newest))
                {
                    stats.NewestTimeStamp = newest;
                }

                return stats;
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
