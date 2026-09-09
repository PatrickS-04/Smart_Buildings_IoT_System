using System;
using System.Collections.Generic;
using System.Text;

namespace PROG7312_POE.Shared.DTOs
{
    public class AttachmentsForSensorsDto
    {
        public string NameOfFile { get; set; } = string.Empty;
        public long SizeOfFile { get; set; }
        public string ContentType {  get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
    }
}
