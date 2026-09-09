using Backend_API.Services;
using PROG7312_POE.Shared.DTOs;

namespace Backend_API.Endpoints
{
    public static class SensorEndpoints
    {
        public static void MapSensorEndpoints(this WebApplication application)
        {
            var group = application.MapGroup("api/sensors");

            group.MapPost("/", (SensorRegistrationDto sensor, SensorRegistrationService sensorService, BuildingConfigurationService buildingConfigService) =>
            {
                if (string.IsNullOrWhiteSpace(sensor.DeviceMacAddress))
                {
                    return Results.BadRequest(new { error = "Device Mac Address is required" });
                }

                if (!buildingConfigService.IsDeploymentValid(sensor.MainZone, sensor.SubZone))
                {
                    return Results.BadRequest(new
                    {
                        error = $"{sensor.SubZone} is not a valid Sub-Zone under {sensor.MainZone}"
                    });
                }

                var registeredSensor = sensorService.RegisterSensor(sensor);
                return Results.Created($"/api/sensors/{registeredSensor.DeviceMacAddress}", registeredSensor);
            });

            group.MapGet("/", (SensorRegistrationService sensorService) =>
            {
                return Results.Ok(sensorService.GetAllSensors());
            });

            group.MapGet("/{deviceMacAddress}", (string deviceMacAddress, SensorRegistrationService sensorService) =>
            {
                var sensor = sensorService.GetSensorByMacAddress(deviceMacAddress);
                return sensor is not null ? Results.Ok(sensor) : Results.NotFound();
            });

            group.MapDelete("/{deviceMacAddress}", (string deviceMacAddress, SensorRegistrationService sensorService) =>
            {
                var removedSensor = sensorService.RemoveSensor(deviceMacAddress);
                return removedSensor ? Results.NoContent() : Results.NotFound();
            });
        }
    }
}
