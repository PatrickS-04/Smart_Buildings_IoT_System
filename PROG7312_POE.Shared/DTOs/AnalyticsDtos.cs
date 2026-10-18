using System;
using System.Collections.Generic;
using System.Text;

namespace PROG7312_POE.Shared.DTOs
{
    public class EpisodeDto
    {
        public string DeviceMacAddress { get; set; } = string.Empty;
        public string SensorType { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;

        public string Kind { get; set; } = string.Empty;

        public double Limit { get; set; }

        public DateTime StartUtc { get; set; }

        public DateTime? EndUtc { get; set; }
        public bool IsOngoing { get; set; }

        public double DurationSeconds { get; set; }

        public double TimeToReachSeconds { get; set; }

        public double PeakValue { get; set; }
        public DateTime PeakAtUtc { get; set; }
    }
}
