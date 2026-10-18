using Microsoft.AspNetCore.Mvc.Diagnostics;
using PROG7312_POE.Shared.DTOs;
using PROG7312_POE.Shared.Models;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace Backend_API.Services
{
    public class SimulatedTelemetryService : BackgroundService
    {
        private readonly SensorRegistrationService _sensorRegistrationService;
        private readonly Random _random = new();
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly Dictionary<string, bool> _lastBoolValue = new();

        private readonly DeviceStateService _deviceStateService;
        private readonly CommandService _commandService;

        public SimulatedTelemetryService(SensorRegistrationService sensorRegistrationService, IHttpClientFactory httpClientFactory, 
            DeviceStateService deviceStateService, CommandService commandService)
        {
            _sensorRegistrationService = sensorRegistrationService;
            _httpClientFactory = httpClientFactory;
            _deviceStateService = deviceStateService;
            _commandService = commandService;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
            var client = _httpClientFactory.CreateClient("TimerApi");

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                foreach (var sensor in _sensorRegistrationService.GetAllSensors())
                {
                    if (_deviceStateService.IsSilenced(sensor.DeviceMacAddress))
                    {
                        continue;
                    }
                    
                    var request = BuildTelemetryIngestionRequest(sensor);
                    
                    if (request is null)
                    {
                        continue;
                    }

                    try
                    {
                        await client.PostAsJsonAsync("/api/telemetrypacket", request, cancellationToken);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine($"Simulator failed to post reading: {sensor.DeviceMacAddress}: {exception.Message}");
                    }
                }
            }
        }

        private TelemetryIngestionRequest? BuildTelemetryIngestionRequest(SensorRegistrationDto sensorRegistrationDto)
        {
            if (ProfileOfSensorSignal.IsCyclic(sensorRegistrationDto.SensorType))
            {
                return CyclicRequest(sensorRegistrationDto);
            }

            return sensorRegistrationDto.SensorType switch
            {
                "Motion (PIR)" or "Presence" => BoolRequest(sensorRegistrationDto, flipProbability: 0.15),
                "Door and Window Contacts" => BoolRequest(sensorRegistrationDto, flipProbability: 0.03),
                "Glass Break" or "Water Leak" or "Smoke and Fire" => BoolRequest(sensorRegistrationDto, flipProbability: 0.02),
                "Smart Relay" or "Water Pump" or "Valve" or "Ventilation Fan" => ActuatorRequest(sensorRegistrationDto),
                _ => null
            };
        }

        private TelemetryIngestionRequest CyclicRequest(SensorRegistrationDto sensorRegistrationDto)
        {
            var value = ProfileOfSensorSignal.ValueAt(
                sensorRegistrationDto.SensorType, sensorRegistrationDto.DeviceMacAddress, DateTime.UtcNow, _random);

            var isCount = sensorRegistrationDto.SensorType == "People Counting";

            return new TelemetryIngestionRequest
            {
                DeviceMacAddress = sensorRegistrationDto.DeviceMacAddress,
                MetricName = sensorRegistrationDto.SensorType,
                Unit = ProfileOfSensorSignal.UnitOf(sensorRegistrationDto.SensorType),
                TelemetryValue = isCount ? TelemetryValue.Int : TelemetryValue.Float,
                NumericValue = isCount ? Math.Round(value) : Math.Round(value, 3)

            };
        }

        private TelemetryIngestionRequest BoolRequest(SensorRegistrationDto sensorRegistrationDto, double flipProbability)
        {
            var key = sensorRegistrationDto.DeviceMacAddress + sensorRegistrationDto.SensorType;
            var currentBoolValue = _lastBoolValue.GetValueOrDefault(key, false);

            if (_random.NextDouble() < flipProbability)
            {
                currentBoolValue = !currentBoolValue;
            }

            _lastBoolValue[key] = currentBoolValue;

            return new TelemetryIngestionRequest
            {
                DeviceMacAddress = sensorRegistrationDto.DeviceMacAddress,
                MetricName = sensorRegistrationDto.SensorType,
                Unit = "",
                TelemetryValue = TelemetryValue.Bool,
                BoolValue = currentBoolValue
            };
        }

        private TelemetryIngestionRequest ActuatorRequest(SensorRegistrationDto sensorRegistrationDto)
        {
            return new TelemetryIngestionRequest
            {
                DeviceMacAddress = sensorRegistrationDto.DeviceMacAddress,
                MetricName = sensorRegistrationDto.SensorType,
                Unit = "",
                TelemetryValue = TelemetryValue.Bool,
                BoolValue = _commandService.GetStateOfCommand(sensorRegistrationDto.DeviceMacAddress)
            };
        }
    }
}
