using System;
using System.Collections.Generic;
using System.Text;

namespace PROG7312_POE.Shared
{
    public static class SensorCategorization
    {
        public static readonly string[] SensorCategories =
        {
            "Environmental", "Occupancy and Motion", "Energy and Power Consumption", "Safety and Security", "Structural and Facility"
        };

        public static readonly Dictionary<string, string[]> SensorTypesByCategory = new()
        {
            ["Environmental"] = new[] { "Temperature", "Humidity", "CO2 (Carbon Dioxide)", "Air Quality (VOCs)", "Light Level (Lux)" },
            ["Occupancy and Motion"] = new[] { "Motion (PIR)", "Presence", "People Counting" },
            ["Energy and Power Consumption"] = new[] { "Power Meters", "Energy Sub-meters", "Utility Meters" },
            ["Safety and Security"] = new[] { "Smoke and Fire", "Water Leak", "Door and Window Contacts", "Glass Break" },
            ["Structural and Facility"] = new[] { "Vibration", "Pipe Pressure" },
        };
    }
}
