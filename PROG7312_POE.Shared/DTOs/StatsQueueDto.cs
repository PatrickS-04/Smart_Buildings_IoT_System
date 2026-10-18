using System;
using System.Collections.Generic;
using System.Text;

namespace PROG7312_POE.Shared.DTOs
{
    public class StatsQueueDto
    {
        public int DepthOfStandardQueue { get; set; }
        public int DepthOfCriticalQueue { get; set; }
        public long TotalStandardEnqueued { get; set; }
        public long TotalCriticalEnqueued { get; set; }
        public long TotalProcessed { get; set; }

        public List<PacketProcessedDto> ProcessedRecently { get; set; } = new();
    }

    public class PacketProcessedDto
    {
        public long Sequence { get; set; }
        public string DeviceMacAddress { get; set; } = string.Empty;
        public string MetricName { get; set; } = string.Empty;
        public bool WasCritical { get; set; }
        public DateTime ProcessedAt { get; set; }
    }
}
