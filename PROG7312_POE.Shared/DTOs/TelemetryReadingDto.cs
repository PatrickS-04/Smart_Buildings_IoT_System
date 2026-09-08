using PROG7312_POE.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PROG7312_POE.Shared.DTOs
{
    public class TelemetryReadingDto
    {
        public string DeviceMacAddress { get; set; } = string.Empty;
        public string MetricName {  get; set; } = string.Empty;
        public string Unit {  get; set; } = string.Empty;
        public DateTime TimeStamp { get; set; }
        public object Value { get; set; } = default!;

        public static TelemetryReadingDto FromPacket(ITelemetryPacketModel telemetryPacket) => new()
        {
            DeviceMacAddress = telemetryPacket.DeviceMacAddress,
            MetricName = telemetryPacket.MetricName,
            Unit = telemetryPacket.Unit,
            TimeStamp = telemetryPacket.TimeStamp,
            Value = telemetryPacket.ValueAsAnObject
        };
    }
}
