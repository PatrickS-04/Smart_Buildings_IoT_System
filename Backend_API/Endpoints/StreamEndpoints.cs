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
        }
    }
}
