namespace Backend_API.Data_Structures
{
    public class ResultOfMst
    {
        // The links kept in the tree
        public List<EdgeOfGraph> Edges { get; } = new();

        public double TotalWeight { get; set; }

        // For each node in the tree, the node one step closer to the root
        public Dictionary<string, string> Parent { get; } = new();

        // How many vertices the tree connects, including the root
        public int VertexCount { get; set; }

        // How many candidate links were pushed onto the heap
        public int HeapPushes { get; set; }
    }

    public class MinimumSpanningTree
    {
        // Prim's algorithm. Grow a tree outwards from the root: out of all the links that leave the tree,
        // always take the cheapest one, and the node it reaches joins the tree
        // A PriorityQueue hands back the cheapest candidate link in O(log Edges),
        // so the whole algorithm is O(E log Vertices).
        // Vertices that isAllowed rejects are left out
        public static ResultOfMst Prim(Graph graph, string root, Func<string, bool> isAllowed)
        {
            var result = new ResultOfMst();

            if (!graph.HasVertex(root) || !isAllowed(root))
            {
                return result;
            }

            var inTree = new HashSet<string> { root };

            // The priority is cost and arrival number, so equal cost links come out in the order they were found
            var heap = new PriorityQueue<EdgeOfGraph, (double Cost, long Order)>();
            long order = 0;

            void PushLinksFrom(string vertex)
            {
                foreach (var edge in graph.NeighboursOf(vertex))
                {
                    if (!inTree.Contains(edge.To) && isAllowed(edge.To))
                    {
                        heap.Enqueue(edge, (edge.Weight, order++));
                        result.HeapPushes++;
                    }
                }
            }

            PushLinksFrom(root);

            while (heap.Count > 0)
            {
                var cheapest = heap.Dequeue();

                // Another link already brought this node into the tree so this one would make a loop
                if (!inTree.Add(cheapest.To))
                {
                    continue;
                }

                result.Edges.Add(cheapest);
                result.TotalWeight += cheapest.Weight;
                result.Parent[cheapest.To] = cheapest.From;

                PushLinksFrom(cheapest.To);
            }

            result.VertexCount = inTree.Count;
            return result;
        }
    }
}
