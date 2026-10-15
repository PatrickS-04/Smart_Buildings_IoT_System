using Backend_API.Services;
using PROG7312_POE.Shared;
using PROG7312_POE.Shared.DTOs;
using PROG7312_POE.Shared.Models;

namespace Backend_API.Endpoints
{
    public static class CommandEndpoints
    {
        public static void MapCommandEndpoints(this WebApplication application)
        {
            var group = application.MapGroup("/api/commands");

            group.MapGet("/actuators", (SensorRegistrationService sensorRegistrationService, 
                CommandService commandService, DeviceStateService deviceStateService) =>
            {
                var actuators = sensorRegistrationService.GetAllSensors()
                    .Where(sensor => sensor.Category == SensorCategorization.ActuatorCategory)
                    .OrderBy(sensor => sensor.SensorType)
                    .ThenBy(sensor => sensor.DeviceMacAddress)
                    .Select(sensor => new ActuatorDto
                    {
                        DeviceMacAddress = sensor.DeviceMacAddress,
                        SensorType = sensor.SensorType,
                        MainZone = sensor.MainZone,
                        SubZone = sensor.SubZone,
                        Location = SensorLocation.Format(sensor),
                        CommandedOn = commandService.GetStateOfCommand(sensor.DeviceMacAddress),
                        ReportedOn = deviceStateService.GetByMacAddress(sensor.DeviceMacAddress)?.LastValue as bool?
                    })
                    .ToList();

                return Results.Ok(actuators);
            });

            group.MapPost("/", (CommandRequestDto commandRequest, SensorRegistrationService sensorRegistrationService,
                CommandService commandService, TelemetryIngestionQueueService queueIngestionService) =>
            {
                var sensor = sensorRegistrationService.GetSensorByMacAddress(commandRequest.DeviceMacAddress);

                if (sensor is null)
                {
                    return Results.NotFound(new { error = $"No registered device with MAC Address {commandRequest.DeviceMacAddress}" });
                }

                if (sensor.Category != SensorCategorization.ActuatorCategory)
                {
                    return Results.BadRequest(new { error = $"{sensor.SensorType} is a sensor, not a controllable device" });
                }

                var description = $"Turned {sensor.SensorType} {OnOff(commandRequest.TurnOn)} ({SensorLocation.Format(sensor)})";
                var record = commandService.ExecuteCommand(new[] { sensor }, commandRequest.TurnOn, description);

                if (record is null)
                {
                    return Results.Ok(new { message = $"{sensor.SensorType} is already {OnOff(commandRequest.TurnOn)}, nothing changed" });
                }

                Acknowledge(record, revert: false, queueIngestionService);
                return Results.Ok(record);
            });

            group.MapPost("/bulk", (BulkCommandRequestDto bulkCommandRequest, SensorRegistrationService sensorRegistrationService,
                CommandService commandService, TelemetryIngestionQueueService ingestionQueueService) =>
            {
                var targets = sensorRegistrationService.GetAllSensors()
                    .Where(sensor => sensor.Category == SensorCategorization.ActuatorCategory
                        && sensor.SensorType == bulkCommandRequest.SensorType)
                    .Where(sensor => string.IsNullOrWhiteSpace(bulkCommandRequest.MainZone) || sensor.MainZone == bulkCommandRequest.MainZone)
                    .ToList();

                if (targets.Count == 0)
                {
                    return Results.NotFound(new { error = "No registered controllable devices match that type and zone" });
                }

                var scope = string.IsNullOrWhiteSpace(bulkCommandRequest.MainZone) ? "the whole building" : bulkCommandRequest.MainZone;
                var description = $"Turned all {bulkCommandRequest.SensorType} devices {OnOff(bulkCommandRequest.TurnOn)} in {scope}";

                var record = commandService.ExecuteCommand(targets, bulkCommandRequest.TurnOn, description);

                if (record is null)
                {
                    return Results.Ok(new { message = $"Every matching device is already {OnOff(bulkCommandRequest.TurnOn)}, nothing changed" });
                }

                Acknowledge(record, revert: false, ingestionQueueService);
                return Results.Ok(record);
            });

            group.MapPost("/undo", (CommandService commandService, TelemetryIngestionQueueService ingestionQueueService) =>
            {
                var record = commandService.UndoLastCommand();

                if (record is null)
                {
                    return Results.BadRequest(new { error = "There is no command to undo" });
                }

                Acknowledge(record, revert: true, ingestionQueueService);
                return Results.Ok(record);
            });

            group.MapGet("/history", (CommandService commandService) =>
            {
                return Results.Ok(commandService.GetCommandHistory());
            });
        }

        private static string OnOff(bool turnOn) => turnOn ? "ON" : "OFF";

        private static void Acknowledge(CommandRecordDto record, bool revert, TelemetryIngestionQueueService ingestionQueueService)
        {
            foreach (var change in record.Changes)
            {
                var state = revert ? change.PreviousState : change.NewState;
                ingestionQueueService.StandardEnqueue(new TelemetryPacketModel<bool>(change.DeviceMacAddress, change.SensorType, state, ""));
            }
        }
    }
}
