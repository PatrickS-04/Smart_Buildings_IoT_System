using Backend_API.Services;

namespace Backend_API.Endpoints
{
    public static class SensorDashboardEndpoints
    {
        public static void MapSensorDashboardEndpoints(this WebApplication application)
        {
            application.MapGet("/api/dashboard", (string? sensorZone, SensorDashboardAggregationService aggregationService) =>
            {
                return Results.Ok(aggregationService.BuildSnapshot(sensorZone ?? ""));
            });
        }
    }
}
