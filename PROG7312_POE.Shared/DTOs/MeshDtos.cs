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
}
