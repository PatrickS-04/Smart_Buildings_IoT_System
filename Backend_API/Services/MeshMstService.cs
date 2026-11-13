using Backend_API.Data_Structures;
using PROG7312_POE.Shared.DTOs;

namespace Backend_API.Services
{
    public class MeshMstService
    {
        private readonly MeshTopologyService _meshTopologyService;

        public MeshMstService(MeshTopologyService meshTopologyService)
        {
            _meshTopologyService = meshTopologyService;
        }

        public MstDto GetMst()
        {
            var topology = _meshTopologyService.Build();
            var graph = topology.Graph;
            var gateway = MeshTopologyService.GatewayId;

            bool IsOnline(string id) => topology.Nodes[id].IsOnline;

            // Only working nodes take part, so a failed node drops out and the treeis rebuilt around it
            var mst = MinimumSpanningTree.Prim(graph, gateway, IsOnline);
            var bfs = GraphTraversal.Bfs(graph, gateway, IsOnline);

            // Every link where both ends are working: the cost of keeping the whole mesh switched on
            var activeLinks = graph.Edges().Where(edge => IsOnline(edge.From) && IsOnline(edge.To)).ToList();
            var activeCost = activeLinks.Sum(edge => edge.Weight);

            // The links the fewest hop routes actually use: each node's link towards the gateway
            var bfsTreeCost = bfs.Parent.Sum(pair => WeightOf(graph, pair.Key, pair.Value));

            var sensorCount = topology.Nodes.Values.Count(node => !node.IsGateway);
            var sensorsInTree = Math.Max(0, mst.VertexCount - 1);

            var routes = mst.Parent.Keys
                .OrderBy(id => id, StringComparer.Ordinal)
                .Select(id =>
                {
                    var (mstHops, mstCost) = PathToRoot(graph, mst.Parent, id);
                    var (bfsHops, bfsCost) = PathToRoot(graph, bfs.Parent, id);

                    return new MstRouteComparisonDto
                    {
                        DeviceMacAddress = id,
                        Label = topology.Nodes[id].Label,
                        MstHops = mstHops,
                        MstPathCost = Math.Round(mstCost, 1),
                        BfsHops = bfsHops,
                        BfsPathCost = Math.Round(bfsCost, 1)
                    };
                })
                .ToList();

            return new MstDto
            {
                GatewayId = gateway,
                NodesInTree = sensorsInTree,
                NodesExcluded = sensorCount - sensorsInTree,
                ActiveLinkCount = activeLinks.Count,
                ActiveMeshCost = Math.Round(activeCost, 1),
                MstLinkCount = mst.Edges.Count,
                MstTotalCost = Math.Round(mst.TotalWeight, 1),
                BfsTreeLinkCount = bfs.Parent.Count,
                BfsTreeCost = Math.Round(bfsTreeCost, 1),
                SavingVsActiveMeshPercent = SavingPercent(mst.TotalWeight, activeCost),
                SavingVsBfsTreePercent = SavingPercent(mst.TotalWeight, bfsTreeCost),
                HeapPushes = mst.HeapPushes,
                Links = mst.Edges.Select(edge => new MeshLinkDto
                {
                    From = edge.From,
                    To = edge.To,
                    Cost = edge.Weight,
                    Distance = Math.Round(Distance(topology.Nodes[edge.From], topology.Nodes[edge.To]), 1)
                }).ToList(),
                Routes = routes
            };
        }

        // HELPERS

        private static double WeightOf(Graph graph, string a, string b)
        {
            return graph.NeighboursOf(a).First(edge => edge.To == b).Weight;
        }

        // Walks from a node back to the root through a parent map, counting hops and adding up link costs
        private static (int Hops, double Cost) PathToRoot(Graph graph, Dictionary<string, string> parent, string node)
        {
            var hops = 0;
            var cost = 0.0;
            var current = node;

            while (parent.TryGetValue(current, out var next))
            {
                cost += WeightOf(graph, current, next);
                current = next;
                hops++;
            }

            return (hops, cost);
        }

        private static double SavingPercent(double mstCost, double otherCost)
        {
            return otherCost > 0 ? Math.Round((1 - mstCost / otherCost) * 100, 1) : 0;
        }

        private static double Distance(MeshNodeDto a, MeshNodeDto b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
