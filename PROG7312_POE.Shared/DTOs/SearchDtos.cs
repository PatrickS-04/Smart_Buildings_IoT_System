using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace PROG7312_POE.Shared.DTOs
{
    public static class AlertStates
    {
        public const string Normal = "normal";
        public const string Warning = "warning";
        public const string Critical = "critical";
        public const string Offline = "offline";

        public static readonly string[] AllAlertStates = { Critical, Offline, Warning, Normal };
    }

    public class DeviceSearchResultDto
    {
        public List<LiveDeviceDto> Devices { get; set; } = new();

        public int TotalMatches { get; set; }
        public int TotalDevices { get; set; }

        public Dictionary<string, int> CountsByAlertState { get; set; } = new();
    }
}
