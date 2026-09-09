using Backend_API.Services;
using PROG7312_POE.Shared.DTOs;

namespace Backend_API.Endpoints
{
    public static class SensorAttachmentEndpoints
    {
        public static void MapSensorAttachmentEndpoints(this WebApplication application)
        {
            var group = application.MapGroup("/api/sensors/{deviceMacAddress}/attachments");

            group.MapPost("/", async (string deviceMacAddress, IFormFileCollection attachedFiles, SensorRegistrationService sensorService, 
                AttachmentsForSensorsService fileAttachmentService) =>
            {
                if (sensorService.GetSensorByMacAddress(deviceMacAddress) is null)
                {
                    return Results.NotFound(new { error = $"There is no sensors registered with Mac Address: {deviceMacAddress}" });
                }

                if (attachedFiles.Count == 0)
                {
                    return Results.BadRequest(new { error = "No files have been uploaded" });
                }

                var savedAttachments = new List<AttachmentsForSensorsDto>();
                var errors = new List<string>();

                foreach (var attachedFile in attachedFiles)
                {
                    var (success, error) = fileAttachmentService.ValidateAttachmentFile(attachedFile);
                    
                    if (!success)
                    {
                        errors.Add(error!);
                        continue;
                    }

                    savedAttachments.Add(await fileAttachmentService.SaveFileAsync(deviceMacAddress, attachedFile));
                }

                return Results.Ok(new { savedAttachments, errors });
            }).DisableAntiforgery();

            group.MapGet("/", (string deviceMacAddress, AttachmentsForSensorsService fileAttachmentService) =>
            {
                return Results.Ok(fileAttachmentService.GetAttachmentsByDevice(deviceMacAddress));
            });
        }
    }
}
