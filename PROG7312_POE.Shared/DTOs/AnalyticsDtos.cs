using System;
using System.Collections.Generic;
using System.Text;

namespace PROG7312_POE.Shared.DTOs
{
    public class EpisodeDto
    {
        public string DeviceMacAddress { get; set; } = string.Empty;
        public string SensorType { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;

        public string Kind { get; set; } = string.Empty;

        public double Limit { get; set; }

        public DateTime StartUtc { get; set; }

        public DateTime? EndUtc { get; set; }
        public bool IsOngoing { get; set; }

        public double DurationSeconds { get; set; }

        public double TimeToReachSeconds { get; set; }

        public double PeakValue { get; set; }
        public DateTime PeakAtUtc { get; set; }
    }

    public class PredictionDto
    {
        public string Kind { get; set; } = string.Empty;
        public double Limit { get; set; }

        public bool InProgress { get; set; }
        public DateTime? CrrentExpectedEnd { get; set; }

        public DateTime PredictedStart { get; set; }
        public DateTime PredictedEnd { get; set; }

        public double CycleSeconds { get; set; }
        public double ExpectedDurationInSeconds { get; set; }
        public double ExpectedTimeToReachInSeconds { get; set; }
        public double ExpectedPeak { get; set; }

        public int AnalysedEpisode { get; set; }
        public int ConfidencePercentage { get; set; }

        public string ConfidenceLabel { get; set; }
    }

    public class TrendWarningDto
    {
        public string Kind { get; set; } = string.Empty;
        public double Limit { get; set; }
        public double SecondsUntilLimitCrossed { get; set; }
    }

    public class SensorForecastDto
    {
        public string DeviceMacAddress { get; set; } = string.Empty;
        public string SensorType { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string MainZone { get; set; } = string.Empty;

        public double CurrentValue { get; set; }

        public string Trend { get; set; } = "steady";
        public double SlopePerMinute { get; set; }

        public bool HasEnoughData { get; set; }
        public List<PredictionDto> Predictions { get; set; } = new();
        public TrendWarningDto? TrendWarning { get; set; }

        public DateTime? NextEvent { get; set; }

        public string Insight { get; set; } = string.Empty;
    }
}
