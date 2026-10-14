using PROG7312_POE.Shared;
using PROG7312_POE.Shared.Models;

namespace Backend_API.Services
{
    public static class TelemetryPriorityClassifier
    {
        public static int? GetCriticalPriority(ITelemetryPacketModel telemetryPacket)
        {
            if (telemetryPacket.ValueAsAnObject is bool flag)
            {
                return flag && SensorThresholds.CriticalWhenTrue.Contains(telemetryPacket.MetricName) ? 0 : null;
            }

            if (!SensorThresholds.Critical.TryGetValue(telemetryPacket.MetricName, out var band))
            {
                return null;
            }

            var value = Convert.ToDouble(telemetryPacket.ValueAsAnObject);
            var isCritical = (band.High is not null && value > band.High) || (band.Low is not null && value < band.Low);

            return isCritical ? 1 : null;
        }
    }
}
