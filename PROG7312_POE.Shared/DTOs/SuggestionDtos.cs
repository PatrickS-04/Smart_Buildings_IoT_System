using System;
using System.Collections.Generic;
using System.Text;

namespace PROG7312_POE.Shared.DTOs
{
    public class InsightDto
    {
        public string Severity { get; set; } = "info";
        public string Message { get; set; } = string.Empty;
    }

    public class SuggestedActionDto
    {
        public string Id { get; set; } = string.Empty;

        public string Kind { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public int Score { get; set; }
        public int EvidenceCount { get; set; }

        public string ActionType { get; set; } = string.Empty;

        public string FilterText { get; set; } = string.Empty;
        public string FilterCategory { get; set; } = string.Empty;
        public string FilterAlertState { get; set; } = string.Empty;
        public string FilterZone { get; set; } = string.Empty;

        public string SensorType { get; set; } = string.Empty;
        public string MainZone { get; set; } = string.Empty;
        public bool TurnOn { get; set; }
    }

    public class SuggestionsDto
    {
        public List<SuggestedActionDto> SuggestedActions { get; set; }
        public List<InsightDto> Insights { get; set; } = new();
        public int TrackedSearches { get; set; }
        public int TrackedCommands { get; set; }
    }
}
