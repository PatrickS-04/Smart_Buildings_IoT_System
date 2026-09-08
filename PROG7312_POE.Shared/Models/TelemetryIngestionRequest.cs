using System;
using System.Collections.Generic;
using System.Text;

namespace PROG7312_POE.Shared.Models
{
    public enum TelemetryValue { Float, Int, Bool }
    public class TelemetryIngestionRequest
    {
        public string DeviceMacAddress { get; set; } = string.Empty;
        public string MetricName { get; set; } = string.Empty;
        public string Unit {  get; set; } = string.Empty;
        public TelemetryValue TelemetryValue { get; set; }
        public double NumericValue { get; set; }
        public bool BoolValue { get; set; }
    }
}
