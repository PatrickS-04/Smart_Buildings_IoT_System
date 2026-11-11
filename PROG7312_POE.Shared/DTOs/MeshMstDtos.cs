using System;
using System.Collections.Generic;
using System.Text;

namespace PROG7312_POE.Shared.DTOs
{
    // How one sensor's route to the gateway compares under the two routing plans
    public class MstRouteComparisonDto
    {
        public string DeviceMacAddress { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;

        // Following only the links kept by the MST
        public int MstHops { get; set; }
        public double MstPathCost { get; set; }

        // Following the fewest hop route
        public int BfsHops { get; set; }
        public double BfsPathCost { get; set; }
    }

    public class MstDto
    {
        public string GatewayId { get; set; } = string.Empty;

        // Sensors in the tree and sensors left out
        public int NodesInTree { get; set; }
        public int NodesExcluded { get; set; }

        // Total power cost of every working link, of the MST, and of fewest hop routes
        public int ActiveLinkCount { get; set; }
        public double ActiveMeshCost { get; set; }

        public int MstLinkCount { get; set; }
        public double MstTotalCost { get; set; }

        public int BfsTreeLinkCount { get; set; }
        public double BfsTreeCost { get; set; }

        public double SavingVsActiveMeshPercent { get; set; }
        public double SavingVsBfsTreePercent { get; set; }

        // Candidate links pushed onto the heap while building the tree
        public int HeapPushes { get; set; }

        public List<MeshLinkDto> Links { get; set; } = new();
        public List<MstRouteComparisonDto> Routes { get; set; } = new();
    }
}
