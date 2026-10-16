using System;
using System.Collections.Generic;
using System.Text;

namespace PROG7312_POE.Shared.DTOs
{
    public class CommandRequestDto
    {
        public string DeviceMacAddress { get; set; } = string.Empty;
        public bool TurnOn { get; set; }
    }

    public class BulkCommandRequestDto
    {
        public string SensorType { get; set; } = string.Empty;

        public string MainZone { get; set; } = string.Empty;
        public bool TurnOn { get; set; }
    }

    public class CommandChangeDto
    {
        public string DeviceMacAddress { get; set; } = string.Empty;
        public string SensorType { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool PreviousState { get; set; }
        public bool NewState { get; set; }
    }

    public class CommandRecordDto
    {
        public long Id { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<CommandChangeDto> Changes { get; set; } = new();
    }

    public class CommandLogEntryDto
    {
        public long Sequence { get; set; }
        public DateTime TimeStamp { get; set; }

        public string Kind { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class CommandHistoryDto
    {
        public List<CommandRecordDto> UndoStack { get; set; } = new();

        public List<CommandLogEntryDto> CommandLog { get; set; } = new();
    }

    public class ActuatorDto
    {
        public string DeviceMacAddress { get; set; } = string.Empty;
        public string SensorType { get; set; } = string.Empty;
        public string MainZone { get; set; } = string.Empty;
        public string SubZone { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;

        public bool CommandedOn { get; set; }
        public bool? ReportedOn { get; set; }
    }
}
