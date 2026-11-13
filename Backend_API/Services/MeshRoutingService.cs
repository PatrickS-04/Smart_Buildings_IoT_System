using Backend_API.Data_Structures;
using PROG7312_POE.Shared.DTOs;

namespace Backend_API.Services
{
    public class MeshRoutingService
    {
        private readonly MeshTopologyService _meshTopologyService;

        public MeshRoutingService(MeshTopologyService meshTopologyService)
        {
            _meshTopologyService = meshTopologyService;
        }

        // Which devices can the gateway still reach, which are down and where could their traffic hop instead
        public ReachabilityDto GetReachability(string? mainZone)
        {
            var topology = _meshTopologyService.Build();
            var search = GraphTraversal.Bfs(topology.Graph, MeshTopologyService.GatewayId, id => topology.Nodes[id].IsOnline);

            var sensors = topology.Nodes.Values
                .Where(node => !node.IsGateway)
                .Where(node => string.IsNullOrWhiteSpace(mainZone) ||
                    string.Equals(node.MainZone, mainZone, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var reachable = sensors
                .Where(node => node.IsOnline && search.Reached(node.DeviceMacAddress))
                .Select(node => Summary(node, search.Hops[node.DeviceMacAddress]))
                .OrderBy(node => node.Hops)
                .ThenBy(node => node.DeviceMacAddress, StringComparer.Ordinal)
                .ToList();

            var isolated = sensors
                .Where(node => node.IsOnline && !search.Reached(node.DeviceMacAddress))
                .Select(node => Summary(node, 0))
                .OrderBy(node => node.DeviceMacAddress, StringComparer.Ordinal)
                .ToList();

            var down = sensors
                .Where(node => !node.IsOnline)
                .OrderBy(node => node.DeviceMacAddress, StringComparer.Ordinal)
                .Select(node => new DownNodeDto
                {
                    DeviceMacAddress = node.DeviceMacAddress,
                    Label = node.Label,
                    MainZone = node.MainZone,
                    HopThrough = topology.Graph.NeighboursOf(node.DeviceMacAddress)
                        .Where(edge => topology.Nodes[edge.To].IsOnline && search.Reached(edge.To))
                        .OrderBy(edge => edge.Weight)
                        .Select(edge => new MeshNeighbourDto
                        {
                            DeviceMacAddress = edge.To,
                            Label = topology.Nodes[edge.To].Label,
                            IsOnline = true,
                            Cost = edge.Weight
                        })
                        .ToList()
                })
                .ToList();

            return new ReachabilityDto
            {
                GatewayId = MeshTopologyService.GatewayId,
                MainZoneFilter = string.IsNullOrWhiteSpace(mainZone) ? null : mainZone,
                TotalNodes = sensors.Count,
                ReachableCount = reachable.Count,
                DownCount = down.Count,
                IsolatedCount = isolated.Count,
                NodesVisited = search.Order.Count,
                Reachable = reachable,
                Down = down,
                Isolated = isolated
            };
        }

        // The fewest-hop route from one device to the gateway, as the mesh is right now
        // Returns null if the device is not part of the mesh
        public RouteDto? GetRoute(string deviceMacAddress)
        {
            var topology = _meshTopologyService.Build();

            if (!topology.Nodes.TryGetValue(deviceMacAddress, out var node))
            {
                return null;
            }

            var gateway = MeshTopologyService.GatewayId;

            // Search 1: as if every node were working. Search 2: only the nodes that really are online
            var normalSearch = GraphTraversal.Bfs(topology.Graph, gateway, _ => true);
            var liveSearch = GraphTraversal.Bfs(topology.Graph, gateway, id => topology.Nodes[id].IsOnline);

            var normalMacAddresses = normalSearch.PathToStart(deviceMacAddress) ?? new List<string>();
            var normalPath = BuildHops(topology, normalMacAddresses);

            var route = new RouteDto
            {
                DeviceMacAddress = deviceMacAddress,
                Label = node.Label,
                IsOnline = node.IsOnline,
                NormalHopCount = Math.Max(0, normalMacAddresses.Count - 1),
                NormalPath = normalPath
            };

            if (node.IsGateway)
            {
                route.RouteFound = true;
                route.Message = "This is the gateway, so there is nowhere further to send data";
                route.Path = normalPath;
                return route;
            }

            if (!node.IsOnline)
            {
                route.RouteFound = false;
                route.Message = "This device is down, so it is not sending anything. Its normal route is shown for reference";
                return route;
            }

            var liveMacAddresses = liveSearch.PathToStart(deviceMacAddress);

            if (liveMacAddresses is null)
            {
                route.RouteFound = false;
                route.Message = "This device is online but every path to the gateway runs through a down node, so its data cannot get out";
                return route;
            }

            route.RouteFound = true;
            route.Path = BuildHops(topology, liveMacAddresses);
            route.HopCount = liveMacAddresses.Count - 1;
            route.TotalCost = Math.Round(route.Path.Sum(hop => hop.CostFromPrevious), 1);
            route.NextHop = route.Path.Count > 1 ? route.Path[1] : null;
            route.ReRouted = !liveMacAddresses.SequenceEqual(normalMacAddresses);
            route.Message = route.ReRouted
                ? "A node on the normnal route is down, so the mesh has found a new path."
                : "Using the normal route.";

            return route;
        }

        // HELPERS

        private static MeshNodeSummaryDto Summary(MeshNodeDto node, int hops) => new()
        {
            DeviceMacAddress = node.DeviceMacAddress,
            Label = node.Label,
            MainZone = node.MainZone,
            Hops = hops
        };

        private static List<MeshHopDto> BuildHops(MeshTopology topology, List<string> macAddresses)
        {
            var hops = new List<MeshHopDto>();

            for (var index = 0; index < macAddresses.Count; index++)
            {
                var node = topology.Nodes[macAddresses[index]];
                var cost = 0.0;

                if (index > 0)
                {
                    cost = topology.Graph.NeighboursOf(macAddresses[index - 1]).First(edge => edge.To == macAddresses[index]).Weight;
                }

                hops.Add(new MeshHopDto
                {
                    Hop = index,
                    DeviceMacAddress = node.DeviceMacAddress,
                    Label = node.Label,
                    IsOnline = node.IsOnline,
                    CostFromPrevious = cost
                });
            }

            return hops;
        }
    }
}
