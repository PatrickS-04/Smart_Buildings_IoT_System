using Backend_API.Services;
using PROG7312_POE.Shared.Models;

namespace Backend_API.Endpoints
{
    public static class StreamEndpoints
    {
        public static void MapStreamEndpoints(this WebApplication application)
        {
            var group = application.MapGroup("/api/stream");

            group.MapGet("/queue-stats", (TelemetryIngestionQueueService queueIngestion) =>
            {
                return Results.Ok(queueIngestion.GetStats());
            });

            group.MapGet("/devices", (DeviceStateService deviceStateService) =>
            {
                return Results.Ok(deviceStateService.GetAll());
            });

            group.MapGet("/devices/{mac}", (string deviceMacAddress, DeviceStateService deviceState) =>
            {
                var device = deviceState.GetByMacAddress(deviceMacAddress);
                return device is not null ? Results.Ok(device) : Results.NotFound();
            });

            group.MapGet("/history/{mac}", (string deviceMacAddress, int? count, TelemetryHistoryService historyService) =>
            {
                return Results.Ok(historyService.GetLatestReading(deviceMacAddress, Math.Clamp(count ?? 20, 1, 500)));
            });

            group.MapGet("/alerts", (AlertStateService alertStateService) =>
            {
                return Results.Ok(alertStateService.GetSnapshot());
            });

            // TESTING
            group.MapPost("/test/flood", (TelemetryIngestionQueueService queueIngestion, int? count) =>
            {
                var total = Math.Clamp(count ?? 20, 1, 50);

                for (var i = 0; i < total; i++)
                {
                    queueIngestion.StandardEnqueue(
                        new TelemetryPacketModel<float>("TEST:FLOOD", "Temperature", 21f + (i % 5) * 0.1f, "°C"));
                }

                queueIngestion.CriticalEnqueue(
                    new TelemetryPacketModel<bool>("TEST:FLOOD", "Smoke and Fire", true, ""), priority: 0);

                return Results.Ok(new { standardEnqueued = total, criticalEnqueued = 1 });
            });

            group.MapPost("/test/disconnect/{mac}", (string deviceMacAddress, SensorRegistrationService sensorRegistrationService, DeviceStateService deviceStateService) =>
            {
                if (sensorRegistrationService.GetSensorByMacAddress(deviceMacAddress) is null)
                {
                    return Results.NotFound(new { error = $"No registered sensor with MAC {deviceMacAddress}" });
                }

                deviceStateService.SetSilenced(deviceMacAddress, true);
                return Results.Ok(new { silenced = deviceMacAddress });
            });

            group.MapPost("/test/reconnect/{mac}", (string deviceMacAddress, DeviceStateService deviceStateService) =>
            {
                deviceStateService.SetSilenced(deviceMacAddress, false);
                return Results.Ok(new { reconnected = deviceMacAddress });
            });
        }
    }
}
