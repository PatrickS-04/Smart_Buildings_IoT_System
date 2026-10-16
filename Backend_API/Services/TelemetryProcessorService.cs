using PROG7312_POE.Shared.DTOs;
using PROG7312_POE.Shared.Models;

namespace Backend_API.Services
{
    public class TelemetryProcessorService : BackgroundService
    {
        private static readonly TimeSpan OfflineAfter = TimeSpan.FromSeconds(35);
        private static readonly TimeSpan OfflineCheckEvery = TimeSpan.FromSeconds(5);

        private readonly TelemetryIngestionQueueService _queueIngestion;
        private readonly TelemetryPacketService _telemetryPacketService;
        private readonly SensorRegistrationService _sensorRegistrationService;
        private readonly DeviceStateService _deviceStateService;
        private readonly TelemetryHistoryService _telemetryHistoryService;
        private readonly AlertStateService _alertStateService;

        public TelemetryProcessorService(TelemetryIngestionQueueService ingestionQueueService, TelemetryPacketService telemetryPacketService, 
            SensorRegistrationService sensorRegistrationService, DeviceStateService deviceStateService, TelemetryHistoryService telemetryHistoryService, 
            AlertStateService alertStateService)
        {
            _queueIngestion = ingestionQueueService;
            _telemetryPacketService = telemetryPacketService;
            _sensorRegistrationService = sensorRegistrationService;
            _deviceStateService = deviceStateService;
            _telemetryHistoryService = telemetryHistoryService;
            _alertStateService = alertStateService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var lastOfflineCheck = DateTime.UtcNow;

            while (!stoppingToken.IsCancellationRequested)
            {
                var processedThisPass = 0;

                while (processedThisPass < 200 && _queueIngestion.TryDequeueNext(out var telemetryPacket, out var wasCritical))
                {
                    ProcessPacket(telemetryPacket!);

                    if (wasCritical)
                    {
                        Console.WriteLine($"[CRITICAL] {telemetryPacket!.DeviceMacAddress} {telemetryPacket.MetricName} = {telemetryPacket.ValueAsAnObject}");
                    }

                    processedThisPass++;
                }

                if (DateTime.UtcNow - lastOfflineCheck >= OfflineCheckEvery)
                {
                    CheckForOfflineDevices();
                    lastOfflineCheck = DateTime.UtcNow;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(250), stoppingToken);
            }
        }

        private void ProcessPacket(ITelemetryPacketModel telemetryPacket)
        {
            _telemetryPacketService.AddTelemetryPacket(telemetryPacket);
            _telemetryHistoryService.Add(telemetryPacket);

            var sensor = _sensorRegistrationService.GetSensorByMacAddress(telemetryPacket.DeviceMacAddress);
            
            if (sensor is null)
            {
                return;
            }

            var state = _deviceStateService.Update(sensor, telemetryPacket);
            UpdateAlerts(sensor, state);
        }

        private void UpdateAlerts(SensorRegistrationDto sensor, LiveDeviceDto state)
        {
            var deviceMac = sensor.DeviceMacAddress;
            var location = SensorLocation.Format(sensor);
            var description = $"{sensor.SensorType} reading ({FormatValue(state)})";

            _alertStateService.Resolve(deviceMac, "OFFLINE", sensor.SensorType, location);

            switch (state.Severity)
            {
                case "critical":
                    _alertStateService.TryActivate(deviceMac, "CRITICAL", sensor.SensorType, location, $"{description} is critical");
                    _alertStateService.Resolve(deviceMac, "WARNING", sensor.SensorType, location);
                    break;

                case "warn":
                    _alertStateService.TryActivate(deviceMac, "WARNING", sensor.SensorType, location, $"{description} is out of range");
                    _alertStateService.Resolve(deviceMac, "CRITICAL", sensor.SensorType, location);
                    break;

                default:
                    _alertStateService.Resolve(deviceMac, "CRITICAL", sensor.SensorType, location);
                    _alertStateService.Resolve(deviceMac, "WARNING", sensor.SensorType, location);
                    break;
            }
        }

        private void CheckForOfflineDevices()
        {
            var now = DateTime.UtcNow;

            foreach (var device in _deviceStateService.GetAll())
            {
                var sensor = _sensorRegistrationService.GetSensorByMacAddress(device.DeviceMacAddress);

                if (sensor is null)
                {
                    _deviceStateService.Remove(device.DeviceMacAddress);
                    _alertStateService.ResolveAll(device.DeviceMacAddress);
                    continue;
                }

                var silentFor = now - device.LastSeen;

                if (silentFor > OfflineAfter)
                {
                    _deviceStateService.MarkOffline(device.DeviceMacAddress);

                    _alertStateService.TryActivate(device.DeviceMacAddress, "OFFLINE", sensor.SensorType, 
                        SensorLocation.Format(sensor), $"No data received for {silentFor.TotalSeconds:0}s");
                }
            }
        }

        private static string FormatValue(LiveDeviceDto state) => state.LastValue switch
        {
            bool flag => flag ? "triggered" : "normal",
            null => "no data",
            var value => $"{Convert.ToDouble(value):0.##} {state.Unit}".Trim()
        };
    }
}
