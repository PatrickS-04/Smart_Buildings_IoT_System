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

        private readonly Dictionary<string, double> _lastFloatValue = new();
        private readonly Dictionary<string, int> _lastIntValue = new();
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

        private TelemetryIngestionRequest? BuildTelemetryIngestionRequest(SensorRegistrationDto sensorRegistrationDto) => sensorRegistrationDto.SensorType switch
        {
            "Temperature" => FloatRequest(sensorRegistrationDto, seed: 21, step: 0.4, min: 15, max: 32, unit: "°C"),
            "Humidity" => FloatRequest(sensorRegistrationDto, seed: 45, step: 1.2, min: 25, max: 70, unit: "%"),
            "CO2 (Carbon Dioxide)" => FloatRequest(sensorRegistrationDto, seed: 600, step: 15, min: 400, max: 1400, unit: "ppm"),
            "Air Quality (VOCs)" => FloatRequest(sensorRegistrationDto, seed: 50, step: 3, min: 0, max: 150, unit: "index"),
            "Light Level (Lux)" => FloatRequest(sensorRegistrationDto, seed: 400, step: 20, min: 100, max: 900, unit: "lux"),
            "Vibration" => FloatRequest(sensorRegistrationDto, seed: 0.02, step: 0.01, min: 0, max: 0.3, unit: "g"),
            "Pipe Pressure" => FloatRequest(sensorRegistrationDto, seed: 4.0, step: 0.1, min: 2, max: 6, unit: "bar"),
            "Power Meters" or "Energy Sub-meters" or "Utility Meters" => FloatRequest(
                sensorRegistrationDto, seed: 3.0, step: 0.5, min: 0.1, max: 25, unit: "kW"),
            "People Counting" => IntRequest(sensorRegistrationDto, seed: 5, jumpProbability: 0.25, min: 0, max: 80),
            "Motion (PIR)" or "Presence" => BoolRequest(sensorRegistrationDto, flipProbability: 0.15),
            "Door and Window Contacts" => BoolRequest(sensorRegistrationDto, flipProbability: 0.03),
            "Glass Break" or "Water Leak" or "Smoke and Fire" => BoolRequest(sensorRegistrationDto, flipProbability: 0.02),
            "Smart Relay" or "Water Pump" or "Valve" or "Ventilation Fan" => ActuatorRequest(sensorRegistrationDto),
            _ => null
        };

        private TelemetryIngestionRequest FloatRequest(SensorRegistrationDto sensorRegistrationDto, double seed, double step, double min, double max, string unit)
        {
            var key = sensorRegistrationDto.DeviceMacAddress + sensorRegistrationDto.SensorType;
            var currentFloatValue = _lastFloatValue.GetValueOrDefault(key, seed);
            var nextFloatValue = Math.Clamp(currentFloatValue + (_random.NextDouble() * 2 - 1) * step, min, max);
            _lastFloatValue[key] = nextFloatValue;

            return new TelemetryIngestionRequest
            {
                DeviceMacAddress = sensorRegistrationDto.DeviceMacAddress,
                MetricName = sensorRegistrationDto.SensorType,
                Unit = unit,
                TelemetryValue = TelemetryValue.Float,
                NumericValue = Math.Round(nextFloatValue, 2)

            };
        }

        private TelemetryIngestionRequest IntRequest(SensorRegistrationDto sensorRegistrationDto, int seed, double jumpProbability, int min, int max)
        {
            var key = sensorRegistrationDto.DeviceMacAddress + sensorRegistrationDto.SensorType;
            var currentIntValue = _lastIntValue.GetValueOrDefault(key, seed);

            if (_random.NextDouble() < jumpProbability)
            {
                currentIntValue = Math.Clamp(currentIntValue + _random.Next(-2, 3), min, max);
            }
            
            _lastIntValue[key] = currentIntValue;

            return new TelemetryIngestionRequest
            {
                DeviceMacAddress = sensorRegistrationDto.DeviceMacAddress,
                MetricName = sensorRegistrationDto.SensorType,
                Unit = "count",
                TelemetryValue = TelemetryValue.Int,
                NumericValue = currentIntValue
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
