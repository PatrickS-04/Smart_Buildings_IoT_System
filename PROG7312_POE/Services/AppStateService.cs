namespace PROG7312_POE.Services
{
    public record AlertItem(string Severity, string Title, string Location, string TimeAgo);

    public class AppStateService
    {
        // Home Dashboard
        public string SelectedZone { get; set; } = string.Empty;
        public List<AlertItem> RecentAlerts { get; } = new();
        public Dictionary<string, string> PreviousSeverityByMacAddress { get; } = new();

        // Command Stream and History page
        public string SearchText { get; set; } = string.Empty;
        public string CategoryFilter { get; set; } = string.Empty;
        public string AlertStateFilter { get; set; } = string.Empty;
        public string ZoneFilter { get; set; } = string.Empty;
    }
}
