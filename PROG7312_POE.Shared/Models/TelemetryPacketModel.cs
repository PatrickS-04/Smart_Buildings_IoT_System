using System;
using System.Collections.Generic;
using System.Text;

namespace PROG7312_POE.Shared.Models
{
    public class TelemetryPacketModel<T> where T : struct
    {
        public string DeviceMacAddress { get; }
        public string MetricName { get; }
        public T Value { get; }
        public string Unit { get; }
        public DateTime TimeStamp { get; }

        public TelemetryPacketModel(string deviceMacAddress, string metricName, T value, string unit, DateTime? timeStamp = null)
        {
            DeviceMacAddress = deviceMacAddress;
            MetricName = metricName;
            Value = value;
            Unit = unit;
            TimeStamp = timeStamp ?? DateTime.UtcNow;
        }

        public override string ToString()
        {
            return $"[{TimeStamp:HH:mm:ss}] {DeviceMacAddress} {MetricName}: {Value}{Unit}";
        }
    }
}
