namespace Backend_API.Data_Structures
{
    public record EdgeOfGraph(string From, string To, double Weight);

    // An undirected, weighted graph stored as an adjacency list
    // each vertex maps to the list of edges touching it
    // Memory is O(Vertices + Edges), and finding a vertex's neighbours is O(degree),
    // which suits a mesh where every node only has a few radio links
    public class Graph
    {
        private readonly Dictionary<string, List<EdgeOfGraph>> _adjacency = new();

        public int VertexCount => _adjacency.Count;
        public int EdgeCount { get; private set; }
        public IEnumerable<string> Vertices => _adjacency.Keys;
        public bool HasVertex(string vertex) => _adjacency.ContainsKey(vertex);
        public void AddVertex(string vertex)
        {
            if (!_adjacency.ContainsKey(vertex))
            {
                _adjacency[vertex] = new List<EdgeOfGraph>();
            }
        }

        public bool HasEdge(string a, string b)
        {
            return _adjacency.TryGetValue(a, out var edges) && edges.Any(edge => edge.To == b);
        }

        // Adds the link in both directions and returns false if already exists
        public bool AddEdge(string a, string b, double weight)
        {
            AddVertex(a);
            AddVertex(b);

            if (a == b || HasEdge(a, b))
            {
                return false;
            }

            _adjacency[a].Add(new EdgeOfGraph(a, b, weight));
            _adjacency[b].Add(new EdgeOfGraph(b, a, weight));
            EdgeCount++;
            return true;
        }

        public IReadOnlyList<EdgeOfGraph> NeighboursOf(string vertex)
        {
            return _adjacency.TryGetValue(vertex, out var edges)
                ? edges
                : Array.Empty<EdgeOfGraph>();
        }

        // Each link once, the adjacency list has each link twice
        public List<EdgeOfGraph> Edges()
        {
            return _adjacency.Values
                .SelectMany(edges => edges)
                .Where(edge => string.CompareOrdinal(edge.From, edge.To) < 0)
                .ToList();
        }
    }
}
