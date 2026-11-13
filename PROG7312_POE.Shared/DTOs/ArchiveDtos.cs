using System;
using System.Collections.Generic;
using System.Text;

namespace PROG7312_POE.Shared.DTOs
{
    public class ArchiveStatsDto
    {
        public string DeviceMacAddress { get; set; } = string.Empty;
        public int ReadingCount { get; set; }

        // How deep the tree actually is
        public int TreeHeight { get; set; }
        public int MinimumPossibleHeight { get; set; }

        public int RotationCount { get; set; }
        public DateTime? OldestTimeStamp { get; set; }
        public DateTime? NewestTimeStamp { get; set; }
    }

    public class  ArchiveQueryResultDto
    {
        public string DeviceMacAddress { get; set; } = string.Empty;
        public DateTime FromTimeStamp { get; set; }
        public DateTime ToTimeStamp { get; set; }

        public int TotalStored { get; set; }
        public int MatchCount { get; set; }
        public int ReturnedCount { get; set; }

        // How many tree nodes were examined to answer the query
        public int NodesVisited { get; set; }

        public List<TelemetryReadingDto> TelemetryReadings { get; set; } = new();
    }
}
