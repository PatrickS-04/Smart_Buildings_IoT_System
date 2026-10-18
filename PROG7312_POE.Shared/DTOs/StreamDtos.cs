using System;
using System.Collections.Generic;
using System.Text;

namespace PROG7312_POE.Shared.DTOs
{
    public class LiveDeviceDto
    {
        public string DeviceMacAddress { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SensorType { get; set; } = string.Empty;
        public string MainZone { get; set; } = string.Empty;
        public string SubZone { get; set; } = string.Empty;
        public string Floor { get; set; } = string.Empty;
        public string RoomOrArea { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public object? LastValue { get; set; }
        public DateTime LastSeen { get; set; }

        public string Severity { get; set; } = string.Empty;
        public bool IsOnline { get; set; } = true;
    }

    public class AlertEventDto
    {
        public long Sequence { get; set; }
        public string DeviceMacAddress { get; set; } = string.Empty;
        public string SensorType { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;

        public string Kind { get; set; } = string.Empty;
        public bool IsResolution { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime TimeStamp { get; set; }
    }

    public class AlertSnapshotDto
    {
        public List<string> ActiveAlertKeys { get; set; } = new();
        public long DuplicatesSuppressed { get; set; }
        public List<AlertEventDto> RecentEvents { get; set; } = new();
    }
}
