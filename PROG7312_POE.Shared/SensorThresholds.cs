using System;
using System.Collections.Generic;
using System.Text;

namespace PROG7312_POE.Shared
{
    public readonly record struct ThresholdBand(double? Low, double? High);
    public static class SensorThresholds
    {
        public static readonly Dictionary<string, ThresholdBand> Warning = new()
        {
            ["Temperature"] = new ThresholdBand(18.5, 25.5),
            ["Humidity"] = new ThresholdBand(30, 60),
            ["CO2 (Carbon Dioxide)"] = new ThresholdBand(null, 1000),
            ["Air Quality (VOCs)"] = new ThresholdBand(null, 100),
            ["Vibration"] = new ThresholdBand(null, 0.15),
            ["Pipe Pressure"] = new ThresholdBand(2.5, 5.5),
            ["Power Meters"] = new ThresholdBand(null, 18),
            ["Energy Sub-meters"] = new ThresholdBand(null, 18),
            ["Utility Meters"] = new ThresholdBand(null, 18),
            ["People Counting"] = new ThresholdBand(null, 60)
        };

        public static readonly Dictionary<string, ThresholdBand> Critical = new()
        {
            ["Temperature"] = new ThresholdBand(16.5, 29),
            ["Humidity"] = new ThresholdBand(null, 65),
            ["CO2 (Carbon Dioxide)"] = new ThresholdBand(null, 1300),
            ["Vibration"] = new ThresholdBand(null, 0.25),
            ["Pipe Pressure"] = new ThresholdBand(2.2, 5.8),
            ["Power Meters"] = new ThresholdBand(null, 22),
            ["Energy Sub-meters"] = new ThresholdBand(null, 22),
            ["Utility Meters"] = new ThresholdBand(null, 22),
        };

        public static readonly HashSet<string> CriticalWhenTrue = new()
        {
            "Smoke and Fire", "Water Leak", "Glass Break"
        };
    }
}
