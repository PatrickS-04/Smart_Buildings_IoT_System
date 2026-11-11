using Backend_API.Services;
using PROG7312_POE.Shared.DTOs;

namespace Backend_API.Endpoints
{
    public static class MeshEndpoints
    {
        public static void MapMeshEndpoints(this WebApplication application)
        {
            var group = application.MapGroup("/api/mesh");

            // Whole mesh, every node, every link, and how many are online
            group.MapGet("/topology", (MeshTopologyService meshTopologyService) =>
            {
                return Results.Ok(meshTopologyService.GetTopologyDto());
            });

            // The nodes one device can talk to directly, with the power cost of each link
            group.MapGet("/node/{deviceMacAddress}/neighbours", (string deviceMacAddress, MeshTopologyService meshTopologyService) =>
            {
                var topology = meshTopologyService.Build();

                if (!topology.Nodes.ContainsKey(deviceMacAddress))
                {
                    return Results.NotFound(new { error = $"{deviceMacAddress} is not part of the mesh" });
                }

                var neighbours = topology.Graph.NeighboursOf(deviceMacAddress)
                    .Select(edge => new MeshNeighbourDto
                    {
                        DeviceMacAddress = edge.To,
                        Label = topology.Nodes[edge.To].Label,
                        IsOnline = topology.Nodes[edge.To].IsOnline,
                        Cost = edge.Weight
                    })
                    .OrderBy(neighbour => neighbour.Cost)
                    .ToList();

                return Results.Ok(neighbours);
            });
        }
    }
}
