using PROG7312_POE.Shared;
using PROG7312_POE.Shared.DTOs;
using PROG7312_POE.Shared.Models;

namespace Backend_API.Services
{
    public class SensorDashboardAggregationService
    {
        private static readonly TimeSpan OfflineThreshold = TimeSpan.FromSeconds(35);
        private static readonly string[] TemperatureSeriesColors = { "#35d0b8", "#f5a623", "#ff6b6b", "#5b9dd9", "#9b8afb", "#e0607a" };
        private static readonly string[] PowerSensorTypes = { "Power Meters", "Energy Sub-meters", "Utility Meters" };

        private readonly SensorRegistrationService _sensorRegistrationService;
        private readonly TelemetryPacketService _telemetryPacketService;

        public SensorDashboardAggregationService(SensorRegistrationService sensorRegistrationService, TelemetryPacketService telemetryPacketService)
        {
            _sensorRegistrationService = sensorRegistrationService;
            _telemetryPacketService = telemetryPacketService;
        }

        public SensorDashboardDto BuildSnapshot(string mainZone)
        {
            var sensors = _sensorRegistrationService.GetAllSensors()
                .Where(sensor => string.IsNullOrEmpty(mainZone) || sensor.MainZone.Trim()
                .Equals(mainZone, StringComparison.OrdinalIgnoreCase)).ToList();

            var latestReadingBySensor = sensors.ToDictionary(
                sensor => sensor.DeviceMacAddress, sensor => _telemetryPacketService.GetTelemetryPacketByDevice(
                    sensor.DeviceMacAddress).LastOrDefault());

            var severityBySensor = sensors.ToDictionary(
                sensor => sensor.DeviceMacAddress, sensor => DetermineSensorSeverity(sensor, latestReadingBySensor[sensor.DeviceMacAddress]));

            var (labelsForEnergy, dataForEnergy) = BuildEnergyBars(sensors, latestReadingBySensor, mainZone);

            return new SensorDashboardDto
            {
                NameOfZone = mainZone,
                TotalNumberOfSensors = sensors.Count,
                NumberOfSensorsOnline = severityBySensor.Values.Count(severity => severity != "critical"),
                SensorOpenAlerts = severityBySensor.Values.Count(severity => severity != ""),
                AverageTemperature = LatestAverage(sensors, latestReadingBySensor, "Temperature"),
                EnergyTodayKwh = EnergyTodayIntegration(sensors),
                TemperatureSeries = BuildTemperatureSeries(sensors),
                LabelsForEnergy = labelsForEnergy,
                DataForEnergy = dataForEnergy,
                SubZoneStatuses = BuildSubZoneStatuses(sensors, severityBySensor),
                MetricGroup = BuildMetricGroups(sensors, latestReadingBySensor)

            };
        }

        private string DetermineSensorSeverity(SensorRegistrationDto sensorRegistrationDto, ITelemetryPacketModel? latestTelemetryPacket)
        {
            if (latestTelemetryPacket is null || DateTime.UtcNow - latestTelemetryPacket.TimeStamp > OfflineThreshold)
            {
                return "critical";
            }

            return SensorSeverityEvaluator.EvaluateSeverity(sensorRegistrationDto.SensorType, latestTelemetryPacket.ValueAsAnObject);
        }

        private double LatestAverage(List<SensorRegistrationDto> registeredSensors, Dictionary<string,
            ITelemetryPacketModel?> latestReadingBySensor, string sensorType)
        {
            var values = registeredSensors.Where(sensor => sensor.SensorType == sensorType)
                .Select(sensor => latestReadingBySensor[sensor.DeviceMacAddress])
                .Where(p => p is not null)
                .Select(p => Convert.ToDouble(p!.ValueAsAnObject))
                .ToList();

            return values.Count > 0 ? Math.Round(values.Average(), 1) : 0;
        }

        private double EnergyTodayIntegration(List<SensorRegistrationDto> sensorRegistrationDto)
        {
            double totalKiloWattHours = 0;

            foreach (var sensor in sensorRegistrationDto.Where(sensor => PowerSensorTypes.Contains(sensor.SensorType)))
            {
                var sensorReadings = _telemetryPacketService.GetTelemetryPacketByDevice(sensor.DeviceMacAddress)
                    .Where(sensorReading => sensorReading.MetricName == sensor.SensorType)
                    .OrderBy(sensorReading => sensorReading.TimeStamp)
                    .ToList();

                for (var i = 1;  i < sensorReadings.Count; i++)
                {
                    var elapsedHours = (sensorReadings[i].TimeStamp - sensorReadings[i - 1].TimeStamp).TotalHours;
                    var kiloWatts = Convert.ToDouble(sensorReadings[i - 1].ValueAsAnObject);
                    totalKiloWattHours += kiloWatts * elapsedHours;
                }
            }

            return Math.Round(totalKiloWattHours, 2);
        }

        private List<TemperatureSeriesDto> BuildTemperatureSeries(List<SensorRegistrationDto> sensorRegistrationDto)
        {
            var temperatureSensors = sensorRegistrationDto.Where(sensor => sensor.SensorType == "Temperature")
                .Take(TemperatureSeriesColors.Length).ToList();
            var result = new List<TemperatureSeriesDto>();

            for (var i = 0; i < temperatureSensors.Count; i++)
            {
                var sensor = temperatureSensors[i];
                var points = _telemetryPacketService.GetTelemetryPacketByDevice(sensor.DeviceMacAddress)
                    .Where(sensorReadings => sensorReadings.MetricName == "Temperature")
                    .OrderBy(sensorReadings => sensorReadings.TimeStamp)
                    .TakeLast(30)
                    .Select(sensorReadings => new TimeStampedValueDto { TimeStamp = sensorReadings.TimeStamp, Value = Convert.ToDouble(sensorReadings.ValueAsAnObject) })
                    .ToList();

                result.Add(new TemperatureSeriesDto
                {
                    TempLabel = $"{sensor.SubZone} - {sensor.DeviceMacAddress}",
                    TempColor = TemperatureSeriesColors[i],
                    PointsInTime = points
                });
            }

            return result;
        }

        private (List<string> EnergyLabels, List<double> EnergyData) BuildEnergyBars(List<SensorRegistrationDto> sensorRegistrationDto, Dictionary<string, ITelemetryPacketModel?> latestReadingsBySensor, string mainZone)
        {
            var groupedSensors = sensorRegistrationDto.Where(sensor => PowerSensorTypes.Contains(sensor.SensorType))
                .GroupBy(sensor => mainZone == "" ? sensor.MainZone : sensor.SubZone)
                .Select(group => new
                {
                    Label = group.Key,
                    Total = group.Sum(sensor => latestReadingsBySensor[sensor.DeviceMacAddress] is { } p ? Convert.ToDouble(p.ValueAsAnObject) : 0),
                })
                .OrderByDescending(group => group.Total)
                .ToList();

            return (groupedSensors.Select(group => group.Label).ToList(), groupedSensors.Select(group => Math.Round(group.Total, 1)).ToList());
        }

        private List<SubZoneStatusDto> BuildSubZoneStatuses(List<SensorRegistrationDto> sensorRegistrationDto, Dictionary<string, string> sensorSeverity)
        {
            return sensorRegistrationDto.GroupBy(sensor => sensor.SubZone)
                .Select(group => new SubZoneStatusDto
                {
                    NameOfSubZone = group.Key,
                    Sensors = group.Select(sensor => new SensorStatusDto
                    {
                        DeviceMacAddress = sensor.DeviceMacAddress,
                        Category = sensor.Category,
                        SensorType = sensor.SensorType,
                        Severity = sensorSeverity[sensor.DeviceMacAddress]
                    }).ToList()
                }).ToList();
        }

        private List<MetricGroupDto> BuildMetricGroups(List<SensorRegistrationDto> sensorRegistrationDto, Dictionary<string, ITelemetryPacketModel?> latestReadingsBySensor)
        {
            var sensorGroups = new List<MetricGroupDto>();

            foreach (var category in SensorCategorization.SensorCategories)
            {
                var metrics = sensorRegistrationDto.Where(sensor => sensor.Category == category)
                    .Select(sensor => new MetricValueDto
                    {
                        MetricLabel = sensor.SensorType,
                        MetricValue = FormatValue(latestReadingsBySensor[sensor.DeviceMacAddress])
                    }).ToList();

                sensorGroups.Add(new MetricGroupDto { CategoryName = category, Metrics = metrics });
            }

            return sensorGroups;
        }

        private static string FormatValue(ITelemetryPacketModel? telemetryPacket)
        {
            if (telemetryPacket is null)
            {
                return "No Data";
            }

            return telemetryPacket.ValueAsAnObject switch
            {
                bool boolean when IsActuator(telemetryPacket.MetricName) => boolean ? "On" : "Off",
                bool boolean => boolean ? "Yes" : "No",
                float f => $"{f:0.#} {telemetryPacket.Unit}".Trim(),
                int integer => $"{integer} {telemetryPacket.Unit}".Trim(),
                _ => telemetryPacket.ValueAsAnObject.ToString() ?? ""
            };
        }

        private static bool IsActuator(string sensorType) =>
            SensorCategorization.SensorTypesByCategory[SensorCategorization.ActuatorCategory].Contains(sensorType);
    }
}
