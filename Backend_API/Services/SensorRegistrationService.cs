using PROG7312_POE.Shared.DTOs;

namespace Backend_API.Services
{
    public class SensorRegistrationService
    {
        private readonly Dictionary<string, SensorRegistrationDto> _sensorDictionary = new();
        private readonly object _lock = new();

        public SensorRegistrationDto RegisterSensor(SensorRegistrationDto sensor)
        {
            lock (_lock)
            {
                _sensorDictionary[sensor.DeviceMacAddress] = sensor;
            }
            return sensor;
        }

        public IReadOnlyList<SensorRegistrationDto> GetAllSensors()
        {
            lock (_lock)
            {
                return _sensorDictionary.Values.ToList();
            }
        }

        public SensorRegistrationDto? GetSensorByMacAddress(string deviceMacAddress)
        {
            lock (_lock)
            {
                return _sensorDictionary.GetValueOrDefault(deviceMacAddress);
            }
        }

        public bool RemoveSensor(string deviceMacAddress)
        {
            lock (_lock)
            {
                return _sensorDictionary.Remove(deviceMacAddress);
            }
        }
    }
}
