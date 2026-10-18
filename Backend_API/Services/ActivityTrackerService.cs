namespace Backend_API.Services
{
    public record SearchActivity(DateTime AtUtc, string Text, string Category, string AlertState, string Zone);

    public record CommandActivity(DateTime AtUtc, string SensorType, string MainZone, bool TurnOn, List<string> TriggeredSensorTypes);
    public class ActivityTrackerService
    {
        private const int MaxItems = 200;

        private readonly List<SearchActivity> _searches = new();
        private readonly List<CommandActivity> _commands = new();
        private readonly object _lock = new();

        public void RecordSearch(string? text, string? category, string? alertState, string? zone)
        {
            var entry = new SearchActivity(DateTime.UtcNow, (text ?? string.Empty).Trim(),
                    category ?? string.Empty, alertState ?? string.Empty, zone ?? string.Empty);

            if (entry.Text.Length == 0 && entry.Category.Length == 0 && entry.AlertState.Length == 0 && entry.Zone.Length == 0)
            {
                return;
            }

            lock (_lock)
            {
                if (_searches.Count > 0)
                {
                    var previous = _searches[^1];
                    var justNow = (entry.AtUtc - previous.AtUtc).TotalSeconds < 5;
                    var sameFilters = previous.Category == entry.Category
                        && previous.AlertState == entry.AlertState
                        && previous.Zone == entry.Zone;

                    if (justNow && sameFilters)
                    {
                        if (entry.Text.Equals(previous.Text, StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }

                        if (previous.Text.Length > 0 && (entry.Text.StartsWith(previous.Text, StringComparison.OrdinalIgnoreCase) ||
                            previous.Text.StartsWith(entry.Text, StringComparison.OrdinalIgnoreCase)))
                        {
                            _searches[^1] = entry;
                            return;
                        }
                    }
                }

                _searches.Add(entry);

                if (_searches.Count > MaxItems)
                {
                    _searches.RemoveAt(0);
                }
            }
        }

        public void RecordCommand(string sensorType, string mainZone, bool turnOn, List<string> triggerSensorTypes)
        {
            lock (_lock)
            {
                _commands.Add(new CommandActivity(DateTime.UtcNow, sensorType, mainZone ?? string.Empty, turnOn, triggerSensorTypes));

                if (_commands.Count > MaxItems)
                {
                    _commands.RemoveAt(0);
                }
            }
        }

        public List<SearchActivity> GetSearches()
        {
            lock (_lock)
            {
                return _searches.ToList();
            }
        }

        public List<CommandActivity> GetCommands()
        {
            lock (_lock)
            {
                return _commands.ToList();
            }
        }
    }
}
