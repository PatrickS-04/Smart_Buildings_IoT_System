using System;
using System.Collections.Generic;
using System.Text;

namespace PROG7312_POE.Shared.DTOs
{
    public class SensorDashboardDto
    {
        public string NameOfZone { get; set; } = "";
        public int NumberOfSensorsOnline { get; set; }
        public int TotalNumberOfSensors { get; set; }
        public int SensorOpenAlerts {  get; set; }
        public double AverageTemperature { get; set; }
        public double EnergyTodayKwh {  get; set; }

        public List<TemperatureSeriesDto> TemperatureSeries { get; set; } = new();
        public List<string> LabelsForEnergy { get; set; } = new();
        public List<double> DataForEnergy { get; set; } = new();

        public List<SubZoneStatusDto> SubZoneStatuses { get; set; } = new();
        public List<MetricGroupDto> MetricGroup { get; set; } = new();
    }

    public class TemperatureSeriesDto
    {
        public string TempLabel { get; set; } = "";
        public string TempColor { get; set; } = "";
        public List<TimeStampedValueDto> PointsInTime { get; set; } = new();
    }

    public class TimeStampedValueDto
    {
        public DateTime TimeStamp { get; set; }
        public double Value { get; set; }
    }

    public class  SubZoneStatusDto
    {
        public string NameOfSubZone { get; set; } = "";
        public List<SensorStatusDto> Sensors { get; set; } = new();
    }

    public class SensorStatusDto
    {
        public string DeviceMacAddress { get; set; } = "";
        public string Category { get; set; } = "";
        public string SensorType { get; set; } = "";
        public string Severity { get; set; } = "";
    }

    public class MetricGroupDto
    {
        public string CategoryName { get; set; } = "";
        public List<MetricValueDto> Metrics { get; set; } = new();
    }

    public class MetricValueDto
    {
        public string MetricLabel { get; set; } = "";
        public string MetricValue { get; set; } = "";
    }
}
