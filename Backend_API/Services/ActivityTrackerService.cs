namespace Backend_API.Services
{
    public record SearchActivity(DateTime AtUtc, string Text, string Category, string AlertState, string Zone);

    public record CommandActivity(DateTime AtUtc, string SensorType, string MainZone, bool TurnOn, List<string> TriggeredSensorTypes);

    /// <summary>
    /// Tracks the user's query/command history in bounded FIFO queues
    /// Newest activity is enqueued at the back; once the cap is reached the oldest
    /// entry is dequeued from the front, so the engine always learns from a sliding
    /// window of recent behaviour
    /// </summary>
    public class ActivityTrackerService
    {
        private const int MaxItems = 200;

        private readonly Queue<SearchActivity> _searches = new();
        private readonly Queue<CommandActivity> _commands = new();
        private SearchActivity? _lastSearch;
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
                if (_lastSearch is not null)
                {
                    var previous = _lastSearch;
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
                            ReplaceNewest(_searches, entry);
                            _lastSearch = entry;
                            return;
                        }
                    }
                }

                _searches.Enqueue(entry);
                _lastSearch = entry;

                while (_searches.Count > MaxItems)
                {
                    _searches.Dequeue();
                }
            }
        }

        public void RecordCommand(string sensorType, string mainZone, bool turnOn, List<string> triggerSensorTypes)
        {
            lock (_lock)
            {
                _commands.Enqueue(new CommandActivity(DateTime.UtcNow, sensorType, mainZone ?? string.Empty, turnOn, triggerSensorTypes));

                while (_commands.Count > MaxItems)
                {
                    _commands.Dequeue();
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

        // A queue has no "replace the last item", so drain it and re-enqueue
        // everything except the newest item, then enqueue the replacement.
        // The queue is capped at 200 items
        private static void ReplaceNewest<T>(Queue<T> queue, T replacement)
        {
            var count = queue.Count;

            for (var index = 0; index < count - 1; index++)
            {
                queue.Enqueue(queue.Dequeue());
            }

            if (count > 0)
            {
                queue.Dequeue();
            }

            queue.Enqueue(replacement);
        }
    }
}
