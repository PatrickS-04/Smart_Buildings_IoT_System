using System;
using System.Collections.Generic;
using System.Text;

namespace PROG7312_POE.Shared.DTOs
{
    public class MeshNodeDto
    {
        public string DeviceMacAddress { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string SensorType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string MainZone { get; set; } = string.Empty;
        public bool IsGateway { get; set; }
        public bool IsOnline { get; set; }

        // Position on a 1000 x 600 map
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class MeshLinkDto
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public double Distance { get; set; }

        // Relative transmit power needed for this link (distance squared)
        public double Cost { get; set; }
    }

    public class  MeshTopologyDto
    {
        public string GatewayId { get; set; } = string.Empty;
        public int NodeCount { get; set; }
        public int OnlineCount { get; set; }
        public int LinkCount { get; set; }

        // Links that had to be added to join otherwise separate clusters
        public int BridgeLinksAdded { get; set; }

        public List<MeshNodeDto> Nodes { get; set; } = new();
        public List<MeshLinkDto> Links { get; set; } = new();
    }

    public class MeshNeighbourDto
    {
        public string DeviceMacAddress { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public double Cost { get; set; }
    }

    // REACHABILITY AND ROUTING

    public class MeshNodeSummaryDto
    {
        public string DeviceMacAddress { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string MainZone { get; set; } = string.Empty;

        // Hops from the gateway
        public int Hops { get; set; }
    }

    public class DownNodeDto
    {
        public string DeviceMacAddress { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string MainZone { get; set; } = string.Empty;

        // Working neighbours that traffic can hop through instead, cheapest link first
        public List<MeshNeighbourDto> HopThrough { get; set; } = new();
    }

    public class ReachabilityDto
    {
        public string GatewayId { get; set; } = string.Empty;
        public string? MainZoneFilter { get; set; }
        public int TotalNodes { get; set; }
        public int ReachableCount { get; set; }
        public int DownCount { get; set; }
        public int IsolatedCount { get; set; }

        // How many nodes the search visited to work this out
        public int NodesVisited { get; set; }

        public List<MeshNodeSummaryDto> Reachable { get; set; } = new();
        public List<DownNodeDto> Down { get; set; } = new();

        // Online but every path to the gateway runs through a down node
        public List<MeshNodeSummaryDto> Isolated { get; set; } = new();
    }

    public class MeshHopDto
    {
        public int Hop { get; set; }
        public string DeviceMacAddress { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public bool IsOnline { get; set; }

        // Power cost of the link from the previous hop
        public double CostFromPrevious { get; set; }
    }

    public class RouteDto
    {
        public string DeviceMacAddress { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public bool RouteFound { get; set; }
        public string Message { get; set; } = string.Empty;

        public int HopCount { get; set; }
        public double TotalCost { get; set; }
        public MeshHopDto? NextHop { get; set; }

        // The route as it is right now, from the sensor to the gateway
        public List<MeshHopDto> Path { get; set; } = new();

        // The route if every node were working and whether the live route is different
        public int NormalHopCount { get; set; }
        public List<MeshHopDto> NormalPath { get; set; } = new();
        public bool ReRouted { get; set; }
    }
}
