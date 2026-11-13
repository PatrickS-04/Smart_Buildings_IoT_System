using Backend_API.Data_Structures;
using PROG7312_POE.Shared;
using PROG7312_POE.Shared.DTOs;

namespace Backend_API.Services
{
    public class MeshTopology
    {
        public Graph Graph { get; init; } = new();
        public Dictionary<string, MeshNodeDto> Nodes { get; init; } = new();
        public int BridgeLinksAdded { get; init; }
    }

    public class MeshTopologyService
    {
        public const string GatewayId = "GATEWAY";

        private const int NeighboursPerNode = 3;
        private const double RadioRange = 300;

        private const double MapWidth = 1000;
        private const double MapHeight = 600;

        private readonly SensorRegistrationService _sensorRegistrationService;
        private readonly DeviceStateService _deviceStateService;

        public MeshTopologyService(SensorRegistrationService sensorRegistrationService, DeviceStateService deviceStateService)
        {
            _sensorRegistrationService = sensorRegistrationService;
            _deviceStateService = deviceStateService;
        }

        public MeshTopology Build()
        {
            var nodes = new Dictionary<string, MeshNodeDto>();

            nodes[GatewayId] = new MeshNodeDto
            {
                DeviceMacAddress = GatewayId,
                Label = "Gateway (Central Server)",
                SensorType = "Gateway",
                Category = "Gateway",
                MainZone = "Technical and Utility",
                IsGateway = true,
                IsOnline = true,
                X = MapWidth / 2,
                Y = MapHeight / 2
            };

            foreach (var sensor in _sensorRegistrationService.GetAllSensors())
            {
                var (x, y) = PositionFor(sensor.DeviceMacAddress, sensor.MainZone);
                var state = _deviceStateService.GetByMacAddress(sensor.DeviceMacAddress);

                nodes[sensor.DeviceMacAddress] = new MeshNodeDto
                {
                    DeviceMacAddress = sensor.DeviceMacAddress,
                    Label = $"{sensor.SensorType} - {sensor.SubZone}",
                    SensorType = sensor.SensorType,
                    Category = sensor.Category,
                    MainZone = sensor.MainZone,
                    IsGateway = false,
                    IsOnline = state?.IsOnline ?? false,
                    X = x,
                    Y = y
                };
            }

            var graph = new Graph();

            foreach(var id in nodes.Keys)
            {
                graph.AddVertex(id);
            }

            LinkNearestNeighbours(graph, nodes);
            var bridges = JoinSeparateClusters(graph, nodes);

            return new MeshTopology { Graph = graph, Nodes = nodes, BridgeLinksAdded = bridges };
        }

        public MeshTopologyDto GetTopologyDto()
        {
            var topology = Build();

            return new MeshTopologyDto
            {
                GatewayId = GatewayId,
                NodeCount = topology.Nodes.Count - 1,
                OnlineCount = topology.Nodes.Values.Count(node => !node.IsGateway && node.IsOnline),
                LinkCount = topology.Graph.EdgeCount,
                BridgeLinksAdded = topology.BridgeLinksAdded,
                Nodes = topology.Nodes.Values.OrderBy(node => !node.IsGateway).ThenBy(node => node.DeviceMacAddress).ToList(),
                Links = topology.Graph.Edges().Select(edge => new MeshLinkDto
                {
                    From = edge.From,
                    To = edge.To,
                    Cost = edge.Weight,
                    Distance = Math.Round(Distance(topology.Nodes[edge.From], topology.Nodes[edge.To]), 1)
                }).ToList()
            };
        }

        // BUILDING THE LINKS
        
        // Every node links to its 3 closest neighbours that are within radio range
        private static void LinkNearestNeighbours(Graph graph, Dictionary<string, MeshNodeDto> nodes)
        {
            foreach (var node in nodes.Values)
            {
                var closest = nodes.Values
                    .Where(other => other.DeviceMacAddress != node.DeviceMacAddress)
                    .Select(other => (Other: other, Distance: Distance(node, other)))
                    .Where(pair => pair.Distance <= RadioRange)
                    .OrderBy(pair => pair.Distance)
                    .ThenBy(pair => pair.Other.DeviceMacAddress, StringComparer.Ordinal)
                    .Take(NeighboursPerNode);

                foreach (var (other, distance) in closest)
                {
                    graph.AddEdge(node.DeviceMacAddress, other.DeviceMacAddress, CostOf(distance));
                }
            }
        }

        // If the mesh fell into separate clusters, connect the two closest nodes
        // from different clusters, and repeat until everything is one mesh
        private static int JoinSeparateClusters(Graph graph, Dictionary<string, MeshNodeDto> nodes)
        {
            var added = 0;

            while (true)
            {
                var components = FindComponents(graph);

                if (components.Count <= 1)
                {
                    return added;
                }

                var bestDistance = double.MaxValue;
                string bestA = string.Empty;
                string bestB = string.Empty;

                for (var first = 0; first < components.Count; first++)
                {
                    for (var second = first + 1; second < components.Count; second++)
                    {
                        foreach (var a in components[first])
                        {
                            foreach (var b in components[second])
                            {
                                var distance = Distance(nodes[a], nodes[b]);

                                if (distance < bestDistance)
                                {
                                    bestDistance = distance;
                                    bestA = a;
                                    bestB = b;
                                }
                            }
                        }
                    }
                }

                graph.AddEdge(bestA, bestB, CostOf(bestDistance));
                added++;
            }
        }

        private static List<List<string>> FindComponents(Graph graph)
        {
            var seen = new HashSet<string>();
            var components = new List<List<string>>();

            foreach (var start in graph.Vertices.OrderBy(vertex => vertex, StringComparer.Ordinal))
            {
                if (seen.Contains(start))
                {
                    continue;
                }

                var component = new List<string>();
                var stack = new Stack<string>();
                stack.Push(start);
                seen.Add(start);

                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    component.Add(current);

                    foreach (var edge in graph.NeighboursOf(current))
                    {
                        if (seen.Add(edge.To))
                        {
                            stack.Push(edge.To);
                        }
                    }
                }

                components.Add(component);
            }

            return components;
        }

        // GEOMETRY

        private static double Distance(MeshNodeDto a, MeshNodeDto b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        // Transmit power grows roughly with distance squared
        private static double CostOf(double distance) => Math.Max(0.1, Math.Round(distance * distance / 1000.0, 1));

        // Each main zone gets a spot on a ring around the gateway
        // and the MAC address nudges the sensor to its own place inside that zone
        private static (double X, double Y) PositionFor(string deviceMacAddress, string mainZone)
        {
            var zones = SensorCategorization.MainZones;
            var zoneIndex = Math.Max(0, Array.IndexOf(zones, mainZone));

            var angle = 2 * Math.PI * zoneIndex / zones.Length;
            var centerX = MapWidth / 2 + 220 * Math.Cos(angle);
            var centerY = MapHeight / 2 + 190 * Math.Sin(angle);

            var hash = StableHash(deviceMacAddress);
            var offsetX = (int)(hash % 161) - 80;
            var offsetY = (int)(hash / 161 % 161) - 80;

            return (Math.Clamp(centerX + offsetX, 20, MapWidth - 20),
                Math.Clamp(centerY + offsetY, 20, MapHeight - 20));
        }

        // FNV-1a hash. Unlike string.GetHashCode it gives the same answer on every run
        // so a sensor always appears in the same place on the map
        private static uint StableHash(string text)
        {
            var hash = 2166136261u;

            foreach (var character in text)
            {
                hash ^= character;
                hash *= 16777619u;
            }

            return hash;
        }
    }
}
