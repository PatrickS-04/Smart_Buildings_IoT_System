using System;
using System.Collections.Generic;
using System.Text;

namespace PROG7312_POE.Shared.Models
{
    public interface ITelemetryPacketModel
    {
        string DeviceMacAddress { get; }
        string MetricName { get; }
        string Unit { get; }
        DateTime TimeStamp { get; }

        object ValueAsAnObject { get; }
    }
}
