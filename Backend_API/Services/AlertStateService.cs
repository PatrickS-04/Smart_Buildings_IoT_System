using PROG7312_POE.Shared.DTOs;

namespace Backend_API.Services
{
    public class AlertStateService
    {
        private const int MaxEventLog = 100;
        private static readonly string[] AllKinds = { "CRITICAL", "WARNING", "OFFLINE" };

        private readonly HashSet<string> _activeAlerts = new();

        private readonly List<AlertEventDto> _recentEvents = new();
        private readonly object _lock = new();

        private long _sequence;
        private long _duplicatesSuppressed;

        private static string Key(string deviceMacAddress, string kind) => $"{deviceMacAddress}|{kind}";

        public bool TryActivate(string deviceMacAddress, string kind, string sensorType, string location, string message)
        {
            lock (_lock)
            {
                if (!_activeAlerts.Add(Key(deviceMacAddress, kind)))
                {
                    _duplicatesSuppressed++;
                    return false;
                }

                AddEvent(deviceMacAddress, kind, sensorType, location, message, isResolution: false);
                return true;
            }
        }

        public bool Resolve(string deviceMacAddress, string kind, string sensorType, string location)
        {
            lock (_lock)
            {
                if (!_activeAlerts.Remove(Key(deviceMacAddress, kind)))
                {
                    return false;
                }

                AddEvent(deviceMacAddress, kind, sensorType, location, $"{kind} cleared", isResolution: true);
                return true;
            }
        }

        public void ResolveAll(string deviceMacAddress)
        {
            lock (_lock)
            {
                foreach (var kind in AllKinds)
                {
                    _activeAlerts.Remove(Key(deviceMacAddress, kind));
                }
            }
        }

        public bool HasActiveAlerts(string deviceMacAddress)
        {
            lock (_lock)
            {
                return AllKinds.Any(kind => _activeAlerts.Contains(Key(deviceMacAddress, kind)));
            }
        }

        public AlertSnapshotDto GetSnapshot()
        {
            lock (_lock)
            {
                return new AlertSnapshotDto
                {
                    ActiveAlertKeys = _activeAlerts.OrderBy(key => key).ToList(),
                    DuplicatesSuppressed = _duplicatesSuppressed,
                    RecentEvents = _recentEvents.ToList()
                };
            }
        }

        private void AddEvent(string deviceMacAddress, string kind, string sensorType, string location, string message, bool isResolution)
        {
            _recentEvents.Insert(0, new AlertEventDto
            {
                Sequence = ++_sequence,
                DeviceMacAddress = deviceMacAddress,
                SensorType = sensorType,
                Location = location,
                Kind = kind,
                IsResolution = isResolution,
                Message = message,
                TimeStamp = DateTime.UtcNow
            });

            if (_recentEvents.Count > MaxEventLog)
            {
                _recentEvents.RemoveAt(_recentEvents.Count - 1);
            }
        }
    }
}
