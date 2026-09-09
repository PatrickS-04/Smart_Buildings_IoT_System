using PROG7312_POE.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PROG7312_POE.Shared.Validation
{
    public static class TreeValidatorForDeployment
    {
        public static bool IsPathValid(ConfigureNode configureNode, IReadOnlyList<string> path, int depth = 0)
        {
            if (depth == path.Count)
            {
                return true;
            }

            var nextSegment = path[depth];
            var matchingChild = configureNode.Children.FirstOrDefault(configNode => string.Equals(configNode.Name, nextSegment, StringComparison.OrdinalIgnoreCase));

            if (matchingChild is null)
            {
                return false;
            }

            return IsPathValid(matchingChild, path, depth + 1);
        }
    }
}
