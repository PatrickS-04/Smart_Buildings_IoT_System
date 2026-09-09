using Backend_API.Services;

namespace Backend_API.Endpoints
{
    public record BufferReadingRequest(double Value, string Unit);

    public static class TelemetryBatchEndpoints
    {
        public static void MapTelemetryBatchEndpoints(this WebApplication application)
        {
            var group = application.MapGroup("/api/telemetry/batch");

            group.MapPost("/{deviceMacAddress}/{metricName}", (string deviceMacAddress, string metricName, BufferReadingRequest bufferRequest, RawTelemetryBatchService telemetryBatchService) =>
            {
                telemetryBatchService.BufferTelemetryReading(deviceMacAddress, metricName, bufferRequest.Unit, bufferRequest.Value);
                return Results.Ok(new { sealedBatches = telemetryBatchService.GetSealedBatchCount(deviceMacAddress, metricName) });
            });

            group.MapPost("/{deviceMacAddress}/{metricName}/send", (string deviceMacAddress, string metricName, RawTelemetryBatchService telemetryBatchService) =>
            {
                var sent = telemetryBatchService.SendToTelemetryPacketService(deviceMacAddress, metricName);
                return Results.Ok(new { sentReadings = sent });
            });
        }
    }
}
