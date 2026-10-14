using PROG7312_POE.Shared;

namespace Backend_API.Services
{
    public static class SensorSeverityEvaluator
    {
        public static string EvaluateSeverity(string sensorType, object value)
        {
            if (value is bool flag)
            {
                if (!flag)
                {
                    return string.Empty;
                }

                if (SensorThresholds.CriticalWhenTrue.Contains(sensorType))
                {
                    return "critical";
                }

                return sensorType == "Door and Window Contacts" ? "warn" : string.Empty;
            }

            var numeric = Convert.ToDouble(value);

            if (SensorThresholds.Critical.TryGetValue(sensorType, out var criticalBand) && IsOutside(criticalBand, numeric))
            {
                return "critical";
            }

            if (SensorThresholds.Warning.TryGetValue(sensorType, out var warningBand) && IsOutside(warningBand, numeric))
            {
                return "warn";
            }

            return string.Empty;
        }

        private static bool IsOutside(ThresholdBand band, double value) =>
            (band.High is not null && value > band.High) || (band.Low is not null && value < band.Low);
    }
}
