using Backend_API.Services;
using System.Security.Cryptography.X509Certificates;

namespace Backend_API.Endpoints
{
    public static class SuggestionEndpoints
    {
        public static void MapSuggestionEndpoints(this WebApplication application)
        {
            var group = application.MapGroup("/api/suggestions");

            group.MapGet("/", (SuggestionEngineService suggestionEngine) =>
            {
                return Results.Ok(suggestionEngine.Build());
            });

            group.MapPost("/test/seed", (ActivityTrackerService activityTracker, string trigger, string actuator, bool turnOn,
                string? zone, int? times) =>
            {
                var repeats = Math.Clamp(times ?? 3, 1, 10);

                for (var index = 0; index < repeats; index++)
                {
                    activityTracker.RecordCommand(actuator, zone ?? string.Empty, turnOn, new List<string> { trigger });
                }

                return Results.Ok(new { seededCommands = repeats, trigger, actuator, turnOn, zone = zone ?? string.Empty });
            });
        }
    }
}
