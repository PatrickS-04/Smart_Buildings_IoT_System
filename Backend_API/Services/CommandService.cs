using PROG7312_POE.Shared.DTOs;

namespace Backend_API.Services
{
    public class CommandService
    {
        private const int MaxLogEntries = 100;

        private readonly Dictionary<string, bool> _stateOfCommand = new();

        private readonly Stack<CommandRecordDto> _undoStack = new();

        private readonly List<CommandLogEntryDto> _logHistory = new();

        private readonly object _lock = new();
        private long _commandId;
        private long _logSequence;

        public bool GetStateOfCommand(string deviceMacAddress)
        {
            lock (_lock)
            {
                return _stateOfCommand.GetValueOrDefault(deviceMacAddress, false);
            }
        }

        public CommandRecordDto? ExecuteCommand(IEnumerable<SensorRegistrationDto> targets, bool turnOn, string description)
        {
            lock (_lock)
            {
                var changes = new List<CommandChangeDto>();

                foreach (var sensor in targets)
                {
                    var previousState = _stateOfCommand.GetValueOrDefault(sensor.DeviceMacAddress, false);

                    if (previousState == turnOn)
                    {
                        continue;
                    }

                    _stateOfCommand[sensor.DeviceMacAddress] = turnOn;

                    changes.Add(new CommandChangeDto
                    {
                        DeviceMacAddress = sensor.DeviceMacAddress,
                        SensorType = sensor.SensorType,
                        Location = SensorLocation.Format(sensor),
                        PreviousState = previousState,
                        NewState = turnOn
                    });
                }

                if (changes.Count == 0)
                {
                    return null;
                }

                var record = new CommandRecordDto
                {
                    Id = ++_commandId,
                    TimeStamp = DateTime.UtcNow,
                    Description = description,
                    Changes = changes
                };

                _undoStack.Push(record);
                AddLog("COMMAND", description);

                return record;
            }
        }

        public CommandRecordDto? UndoLastCommand()
        {
            lock (_lock)
            {
                if (!_undoStack.TryPop(out var record))
                {
                    return null;
                }

                foreach (var change in record.Changes)
                {
                    _stateOfCommand[change.DeviceMacAddress] = change.PreviousState;
                }

                AddLog("UNDO", $"Undid: {record.Description}");
                return record;
            }
        }

        public CommandHistoryDto GetCommandHistory()
        {
            lock (_lock)
            {
                return new CommandHistoryDto
                {
                    UndoStack = _undoStack.ToList(),
                    CommandLog = _logHistory.ToList()
                };
            }
        }

        private void AddLog(string kind, string description)
        {
            _logHistory.Insert(0, new CommandLogEntryDto
            {
                Sequence = ++_logSequence,
                TimeStamp = DateTime.UtcNow,
                Kind = kind,
                Description = description
            });

            if (_logHistory.Count > MaxLogEntries)
            {
                _logHistory.RemoveAt(_logHistory.Count - 1);
            }
        }
    }
}
