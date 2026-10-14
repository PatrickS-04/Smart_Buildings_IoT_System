using PROG7312_POE.Shared.DTOs;
using PROG7312_POE.Shared.Models;

namespace Backend_API.Services
{
    public class DeviceStateService
    {
        private readonly Dictionary<string, LiveDeviceDto> _liveDevices = new();

        private readonly HashSet<string> _silencedDevices = new();
        
        private readonly object _lock = new();

        public LiveDeviceDto Update(SensorRegistrationDto sensor, ITelemetryPacketModel telemetryPacket)
        {
            lock (_lock)
            {
                if (!_liveDevices.TryGetValue(sensor.DeviceMacAddress, out var state))
                {
                    state = new LiveDeviceDto { DeviceMacAddress = sensor.DeviceMacAddress };
                    _liveDevices[sensor.DeviceMacAddress] = state;
                }

                state.Category = sensor.Category;
                state.SensorType = sensor.SensorType;
                state.MainZone = sensor.MainZone;
                state.SubZone = sensor.SubZone;
                state.Floor = sensor.Floor;
                state.RoomOrArea = sensor.RoomOrArea;

                state.Unit = telemetryPacket.Unit;
                state.LastValue = telemetryPacket.ValueAsAnObject;
                state.LastSeen = DateTime.UtcNow;
                state.IsOnline = true;
                state.Severity = SensorSeverityEvaluator.EvaluateSeverity(sensor.SensorType, telemetryPacket.ValueAsAnObject);

                return Copy(state);
            }
        }

        public LiveDeviceDto? GetByMacAddress(string deviceMacAddress)
        {
            lock (_lock)
            {
                return _liveDevices.TryGetValue(deviceMacAddress, out var state) ? Copy(state) : null;
            }
        }

        public List<LiveDeviceDto> GetAll()
        {
            lock (_lock)
            {
                return _liveDevices.Values.Select(Copy).OrderBy(device => device.DeviceMacAddress).ToList();
            }
        }

        public void MarkOffline(string deviceMacAddress)
        {
            lock (_lock)
            {
                if (_liveDevices.TryGetValue(deviceMacAddress, out var state))
                {
                    state.IsOnline = false;
                }
            }
        }

        public void Remove(string deviceMacAddress)
        {
            lock (_lock)
            {
                _liveDevices.Remove(deviceMacAddress);
                _silencedDevices.Remove(deviceMacAddress);
            }
        }

        public void SetSilenced(string deviceMacAddress, bool silenced)
        {
            lock (_lock)
            {
                if (silenced)
                {
                    _silencedDevices.Add(deviceMacAddress);
                }
                else
                {
                    _silencedDevices.Remove(deviceMacAddress);
                }
            }
        }

        public bool IsSilenced(string deviceMacAddress)
        {
            lock (_lock)
            {
                return _silencedDevices.Contains(deviceMacAddress);
            }
        }

        private static LiveDeviceDto Copy(LiveDeviceDto state) => new()
        {
            DeviceMacAddress = state.DeviceMacAddress,
            Category = state.Category,
            SensorType = state.SensorType,
            MainZone = state.MainZone,
            SubZone = state.SubZone,
            Floor = state.Floor,
            RoomOrArea = state.RoomOrArea,
            Unit = state.Unit,
            LastValue = state.LastValue,
            LastSeen = state.LastSeen,
            Severity = state.Severity,
            IsOnline = state.IsOnline
        };
    }
}
