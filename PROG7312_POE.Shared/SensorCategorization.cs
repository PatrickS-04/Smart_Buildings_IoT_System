using System;
using System.Collections.Generic;
using System.Text;

namespace PROG7312_POE.Shared
{
    public static class SensorCategorization
    {
        public const string ActuatorCategory = "Actuators and Controls";

        public static readonly string[] MainZones =
        {
            "Occupied Spaces", "Amenities and Public Areas", "Technical and Utility",
            "Facilities and Logistics", "Structural and Safety", "Exterior and Perimeter"
        };

        public static readonly Dictionary<string, string[]> SubZones = new()
        {
            ["Occupied Spaces"] = new[] { "Open Offices", "Private Offices", "Executive Suites", "Meeting/Conference Rooms" },
            ["Amenities and Public Areas"] = new[] { "Main Lobby", "Lift Lobby", "Corridor/Hallway", "Restrooms", "Cafeteria", "Gym" },
            ["Technical and Utility"] = new[] { "Server Rooms/Data Centers", "Electrical Rooms", "HVAC Plant Rooms" },
            ["Facilities and Logistics"] = new[] { "Loading Docks", "Janitorial Rooms/Closets", "Waste/Storage Depots" },
            ["Structural and Safety"] = new[] { "Emergency Stairwells", "Lift Shafts", "Fire Command Centers" },
            ["Exterior and Perimeter"] = new[] { "Parking Garages", "Rooftop", "Main Entrance" }
        };

        public static readonly string[] SensorCategories =
        {
            "Environmental", "Occupancy and Motion", "Energy and Power Consumption", "Safety and Security", "Structural and Facility", ActuatorCategory
        };

        public static readonly Dictionary<string, string[]> SensorTypesByCategory = new()
        {
            ["Environmental"] = new[] { "Temperature", "Humidity", "CO2 (Carbon Dioxide)", "Air Quality (VOCs)", "Light Level (Lux)" },
            ["Occupancy and Motion"] = new[] { "Motion (PIR)", "Presence", "People Counting" },
            ["Energy and Power Consumption"] = new[] { "Power Meters", "Energy Sub-meters", "Utility Meters" },
            ["Safety and Security"] = new[] { "Smoke and Fire", "Water Leak", "Door and Window Contacts", "Glass Break" },
            ["Structural and Facility"] = new[] { "Vibration", "Pipe Pressure" },
            [ActuatorCategory] = new[] { "Smart Relay", "Water Pump", "Valve", "Ventilation Fan" }
        };
    }
}
