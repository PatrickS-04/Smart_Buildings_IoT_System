using PROG7312_POE.Shared.Models;
using PROG7312_POE.Shared.Validation;

namespace Backend_API.Services
{
    public class BuildingConfigurationService
    {
        public ConfigureNode Root { get; }

        public BuildingConfigurationService()
        {
            Root = new ConfigureNode("Facility 1");

            var occupiedSpaces = Root.AddChild("Occupied Spaces");
            occupiedSpaces.AddChild("Open Offices");
            occupiedSpaces.AddChild("Private Offices");
            occupiedSpaces.AddChild("Executive Suites");
            occupiedSpaces.AddChild("Meeting/Conference Rooms");

            var amenitiesAndPublicAreas = Root.AddChild("Amenities and Public Areas");
            amenitiesAndPublicAreas.AddChild("Main Lobby");
            amenitiesAndPublicAreas.AddChild("Lift Lobby");
            amenitiesAndPublicAreas.AddChild("Corridor/Hallway");
            amenitiesAndPublicAreas.AddChild("Restrooms");
            amenitiesAndPublicAreas.AddChild("Cafeteria");
            amenitiesAndPublicAreas.AddChild("Gym");

            var technicalAndUtility = Root.AddChild("Technical and Utility");
            technicalAndUtility.AddChild("Server Rooms/Data Centers");
            technicalAndUtility.AddChild("Electrical Rooms");
            technicalAndUtility.AddChild("HVAC Plant Rooms");

            var facilitiesAndLogistics = Root.AddChild("Facilities and Logistics");
            facilitiesAndLogistics.AddChild("Loading Docks");
            facilitiesAndLogistics.AddChild("Janitorial Rooms/Closets");
            facilitiesAndLogistics.AddChild("Waste/Storage Depots");

            var structuralAndSafety = Root.AddChild("Structural and Safety");
            structuralAndSafety.AddChild("Emergency Stairwells");
            structuralAndSafety.AddChild("Lift Shafts");
            structuralAndSafety.AddChild("Fire Command Centers");

            var exteriorAndPerimeter = Root.AddChild("Exterior and Perimeter");
            exteriorAndPerimeter.AddChild("Parking Garages");
            exteriorAndPerimeter.AddChild("Rooftop");
            exteriorAndPerimeter.AddChild("Main Entrance");
        }

        public bool IsDeploymentValid(string mainZone, string subZone)
        {
            return TreeValidatorForDeployment.IsPathValid(Root, new[] { mainZone, subZone });
        }
    }
}
