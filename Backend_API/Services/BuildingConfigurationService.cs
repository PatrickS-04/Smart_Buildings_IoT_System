using PROG7312_POE.Shared;
using PROG7312_POE.Shared.Models;
using PROG7312_POE.Shared.Validation;

namespace Backend_API.Services
{
    public class BuildingConfigurationService
    {
        public ConfigureNode Root { get; }

        public BuildingConfigurationService()
        {
            Root = new ConfigureNode("Facility");

            foreach (var mainZone in SensorCategorization.MainZones)
            {
                var nodeForZone = Root.AddChild(mainZone);
                foreach (var subZone in SensorCategorization.SubZones.GetValueOrDefault(mainZone, Array.Empty<string>()))
                {
                    nodeForZone.AddChild(subZone);
                }
            }
        }

        public bool IsDeploymentValid(string mainZone, string subZone)
        {
            return TreeValidatorForDeployment.IsPathValid(Root, new[] { mainZone, subZone });
        }
    }
}
