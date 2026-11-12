namespace PROG7312_POE.Services
{
    public record AlertItem(string Severity, string Title, string Location, DateTime AtUtc);

    public class AppStateService
    {
        // Home Dashboard
        public string SelectedZone { get; set; } = string.Empty;
        public List<AlertItem> RecentAlerts { get; } = new();
        public Dictionary<string, string> PreviousSeverityByMacAddress { get; } = new();

        // Command Stream and History Page
        public string SearchText { get; set; } = string.Empty;
        public string CategoryFilter { get; set; } = string.Empty;
        public string AlertStateFilter { get; set; } = string.Empty;
        public string ZoneFilter { get; set; } = string.Empty;

        // Network Topology and Mesh Routing Page
        public string MeshSelectedDeviceMacAddress { get; set; } = string.Empty;
        public string MeshZoneFilter { get; set; } = string.Empty;
        public bool MeshShowMst { get; set; } = true;
        public DateTime? MeshHistoryFrom { get; set; }
        public DateTime? MeshHistoryTo { get; set; }
    }
}
