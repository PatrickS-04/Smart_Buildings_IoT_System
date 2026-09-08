using System;
using System.Collections.Generic;
using System.Text;

namespace PROG7312_POE.Shared.DTOs
{
    public class SensorRegistrationDto
    {
        public string DeviceMacAddress { get; set; } = string.Empty;
        public string MainZone { get; set; } = string.Empty;
        public string SubZone { get; set; } = string.Empty;
        public string Floor { get; set;  } = string.Empty;
        public string RoomOrArea { get; set;  } = string.Empty;
        public string Category { get; set;  } = string.Empty;
        public string SensorType { get; set;  } = string.Empty;
    }
}
