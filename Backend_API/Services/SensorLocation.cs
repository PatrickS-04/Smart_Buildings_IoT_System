using PROG7312_POE.Shared.DTOs;

namespace Backend_API.Services
{
    public static class SensorLocation
    {
        public static string Format(SensorRegistrationDto sensor) =>
            string.IsNullOrWhiteSpace(sensor.RoomOrArea)
            ? $"{sensor.SubZone}, {sensor.Floor}"
            : $"{sensor.SubZone}, {sensor.Floor}, {sensor.RoomOrArea}";
    }
}
