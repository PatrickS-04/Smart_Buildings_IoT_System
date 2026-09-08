using Backend_API.Services;
using PROG7312_POE.Shared.DTOs;
using PROG7312_POE.Shared.Models;

namespace Backend_API.Endpoints
{
    public static class TelemetryPacketEndpoints
    {
        public static void MapTelemetryPacketEndpoints(this WebApplication application)
        {
            var group = application.MapGroup("/api/telemetrypacket");

            group.MapPost("/", (TelemetryIngestionRequest telemetryPacketRequest, TelemetryPacketService telemetryPacketService) =>
            {
                if (string.IsNullOrWhiteSpace(telemetryPacketRequest.DeviceMacAddress))
                {
                    return Results.BadRequest(new { error = "Device Mac Address is required"});
                }

                ITelemetryPacketModel telemetryPacketModel = telemetryPacketRequest.TelemetryValue switch
                {
                    TelemetryValue.Float => new TelemetryPacketModel<float>(
                        telemetryPacketRequest.DeviceMacAddress, telemetryPacketRequest.MetricName, (float)telemetryPacketRequest.NumericValue, telemetryPacketRequest.Unit),
                    TelemetryValue.Int => new TelemetryPacketModel<int>(
                        telemetryPacketRequest.DeviceMacAddress, telemetryPacketRequest.MetricName, (int)telemetryPacketRequest.NumericValue, telemetryPacketRequest.Unit),
                    TelemetryValue.Bool => new TelemetryPacketModel<bool>(
                        telemetryPacketRequest.DeviceMacAddress, telemetryPacketRequest.MetricName, telemetryPacketRequest.BoolValue, telemetryPacketRequest.Unit),
                    _ => throw new ArgumentOutOfRangeException(nameof(telemetryPacketRequest.TelemetryValue))
                };

                telemetryPacketService.AddTelemetryPacket(telemetryPacketModel);
                return Results.Ok(TelemetryReadingDto.FromPacket(telemetryPacketModel));
            });

            group.MapGet("/{deviceMacAddress}", (string deviceMacAddress, TelemetryPacketService telemetryPacketService) =>
            {
                var telemetryReadings = telemetryPacketService.GetTelemetryPacketByDevice(deviceMacAddress).Select(TelemetryReadingDto.FromPacket);
                return Results.Ok(telemetryReadings);
            });

            group.MapGet("/", (TelemetryPacketService telemetryService) =>
            {
                var telemetryReadings = telemetryService.GetAllDevices().Select(TelemetryReadingDto.FromPacket);
                return Results.Ok(telemetryReadings);
            });
        }
    }
}
