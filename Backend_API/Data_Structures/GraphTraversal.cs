namespace Backend_API.Data_Structures
{

    // What a breadth-first search found
    public class ResultOfBfs
    {
        // How many hops each reached vertex is from the start
        public Dictionary<string, int> Hops { get; } = new();

        // For each reached vertex, the vertex it was discovered from (one step closer to the start)
        public Dictionary<string, string> Parent { get; } = new();

        // Vertices in the order they were visited
        public List<string> Order { get; } = new();

        public bool Reached(string vertex) => Hops.ContainsKey(vertex);

        // The path from a vertex back to the start and null if never reached
        public List<string>? PathToStart(string vertex)
        {
            if (!Reached(vertex))
            {
                return null;
            }

            var path = new List<string> { vertex };

            while (Parent.TryGetValue(path[^1], out var next))
            {
                path.Add(next);
            }

            return path;
        }
    }

    public static class GraphTraversal
    {
        // Breadth-first search: visit everything one hop away, then everything two hops away, and so on
        // Because it spreads outwards evenly, the first time it reaches a vertex is by the fewest-hop route
        // Vertices that isAllowed rejects are treated as if they do not exist (for example a node that is down)
        // Time O(Vertices + Edges): every vertex is queued once and every link is looked at a constant number of times
        public static ResultOfBfs Bfs(Graph graph, string start, Func<string, bool> isAllowed)
        {
            var result = new ResultOfBfs();

            if (!graph.HasVertex(start) || !isAllowed(start))
            {
                return result;
            }

            var queue = new Queue<string>();
            queue.Enqueue(start);
            result.Hops[start] = 0;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                result.Order.Add(current);

                // Cheapest link first, so ties between equal-hop routes always resolve the same way
                var neighbours = graph.NeighboursOf(current)
                    .OrderBy(edge => edge.Weight)
                    .ThenBy(edge => edge.To, StringComparer.Ordinal);

                foreach (var edge in neighbours)
                {
                    if (result.Hops.ContainsKey(edge.To) || !isAllowed(edge.To))
                    {
                        continue;
                    }

                    result.Hops[edge.To] = result.Hops[current] + 1;
                    result.Parent[edge.To] = current;
                    queue.Enqueue(edge.To);
                }
            }

            return result;
        }
    }
}
