using PROG7312_POE.Shared.DTOs;

namespace Backend_API.Services
{
    public class AttachmentsForSensorsService
    {
        private static readonly string[] AllowedFileExtensions =
        {
            ".pdf", ".txt", ".json", ".log", ".config", ".jpeg", ".jpg", ".png", ".xml"
        };

        private const long MaxFileSize = 10 * 1024 * 1024;

        private readonly string _rootPath;
        private readonly Dictionary<string, List<AttachmentsForSensorsDto>> _attachmentsForSensors = new();
        private readonly object _lock = new();

        public AttachmentsForSensorsService(IWebHostEnvironment webHostEnvironment)
        {
            _rootPath = Path.Combine(webHostEnvironment.ContentRootPath, "Application_Data", "SensorAttachments");
            Directory.CreateDirectory(_rootPath);
        }

        private static string CleanDeviceMacAddress(string deviceMacAddress)
        {
            return deviceMacAddress.Replace(":", "-");
        }

        public (bool Success, string? Error) ValidateAttachmentFile(IFormFile attachedFile)
        {
            if (attachedFile.Length == 0)
            {
                return (false, $"{attachedFile.FileName} is empty");
            }

            if (attachedFile.Length > MaxFileSize)
            {
                return (false, $"{attachedFile.FileName} exceeds the file size limit of 10MB");
            }

            var fileExtension = Path.GetExtension(attachedFile.FileName).ToLowerInvariant();
            if (!AllowedFileExtensions.Contains(fileExtension))
            {
                return (false, $"{attachedFile.FileName} has an unsupported file type {fileExtension}");
            }

            return (true, null);
        }

        public async Task<AttachmentsForSensorsDto> SaveFileAsync(string deviceMacAddress, IFormFile attachedFile)
        {
            var folderOfDevice = Path.Combine(_rootPath, CleanDeviceMacAddress(deviceMacAddress));
            Directory.CreateDirectory(folderOfDevice);

            var cleanedFileName = $"{Guid.NewGuid():N}_{Path.GetFileName(attachedFile.FileName)}";
            var newFullPath = Path.Combine(folderOfDevice, cleanedFileName);

            await using (var fileStream = new FileStream(newFullPath, FileMode.Create))
            {
                await attachedFile.CopyToAsync(fileStream);
            }

            var attachmentsDto = new AttachmentsForSensorsDto
            {
                NameOfFile = attachedFile.FileName,
                SizeOfFile = attachedFile.Length,
                ContentType = attachedFile.ContentType,
                UploadedAt = DateTime.UtcNow
            };

            lock (_lock)
            {
                if (!_attachmentsForSensors.TryGetValue(deviceMacAddress, out var list))
                {
                    list = new List<AttachmentsForSensorsDto>();
                    _attachmentsForSensors[deviceMacAddress] = list;
                }
                list.Add(attachmentsDto);
            }
            return attachmentsDto;
        }

        public IReadOnlyList<AttachmentsForSensorsDto> GetAttachmentsByDevice(string deviceMacAddress)
        {
            lock (_lock)
            {
                return _attachmentsForSensors.TryGetValue(deviceMacAddress, out var list) ? list.ToList() : new List<AttachmentsForSensorsDto>();
            }
        }

        public bool RemoveSensorAttachments(string deviceMacAddress)
        {
            var cleanedDeviceMacAddress = CleanDeviceMacAddress(deviceMacAddress);
            string sensorAttachmentsFolderPath = Path.Combine(_rootPath, cleanedDeviceMacAddress);

            bool isFilesDeleted = false;

            if (Directory.Exists(sensorAttachmentsFolderPath))
            {
                Directory.Delete(sensorAttachmentsFolderPath, recursive: true);
                isFilesDeleted = true;
            }
            
            lock (_lock)
            {
                bool isMemoryCleared = _attachmentsForSensors.Remove(deviceMacAddress);
                return isFilesDeleted || isMemoryCleared;
            }
        }
    }
}
