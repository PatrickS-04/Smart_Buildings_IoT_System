namespace Backend_API.Data_Structures
{

    // A self-balancing binary search tree (AVL tree)
    // After every insert, each node's left and right subtrees differ in height by at most 1,
    // so the tree stays about log2(n) levels deep even when keys arrive in sorted order
    // (which is exactly what timestamps do)
    public class AvlTree<TKey, TValue> where TKey : IComparable<TKey>
    {
        private sealed class Node
        {
            public Node(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }

            public TKey Key { get; }
            public TValue Value { get; set; }
            public Node? Left { get; set; }
            public Node? Right { get; set; }
            public int Height { get; set; } = 1;
        }

        private Node? _root;

        public int Count { get; private set; }

        // How many rotations were needed to keep the tree balanced
        public int Rotations { get; private set; }

        public int Height => HeightOf(_root);

        public void Insert(TKey key, TValue value)
        {
            _root = Insert(_root, key, value);
        }

        private Node Insert(Node? node, TKey key, TValue value)
        {
            if (node is null)
            {
                Count++;
                return new Node(key, value);
            }

            var comparison = key.CompareTo(node.Key);

            if (comparison < 0)
            {
                node.Left = Insert(node.Left, key, value);
            }
            else if (comparison > 0)
            {
                node.Right = Insert(node.Right, key, value);
            }
            else
            {
                node.Value = value;
                return node;
            }

            return Rebalance(node);
        }

        // Returns every value whose key is between from and to (inclusive), in key order
        // Whole subtrees that cannot contain matches are skipped, which is why this beats scanning a list
        // nodesVisited reports how many nodes were actually looked at
        public List<TValue> Range(TKey from, TKey to, out int nodesVisited)
        {
            var results = new List<TValue>();
            var visited = 0;

            Collect(_root, from, to, results, ref visited);

            nodesVisited = visited;
            return results;
        }

        private static void Collect(Node? node, TKey from, TKey to, List<TValue> results, ref int visited)
        {
            if (node is null)
            {
                return;
            }

            visited++;

            var againstLow = from.CompareTo(node.Key);
            var againstHigh = to.CompareTo(node.Key);

            if (againstLow < 0)
            {
                Collect(node.Left, from, to, results, ref visited);
            }

            if (againstLow <= 0 && againstHigh >= 0)
            {
                results.Add(node.Value);
            }

            if (againstHigh > 0)
            {
                Collect(node.Right, from, to, results, ref visited);
            }
        }

        public List<TValue> InOrder()
        {
            var results = new List<TValue>(Count);
            Walk(_root, results);
            return results;
        }

        private static void Walk(Node? node, List<TValue> results)
        {
            if (node is null)
            {
                return;
            }

            Walk(node.Left, results);
            results.Add(node.Value);
            Walk(node.Right, results);
        }

        public bool TryGetMinKey(out TKey key)
        {
            var node = _root;

            if (node is null)
            {
                key = default;
                return false;
            }

            while (node.Left is not null)
            {
                node = node.Left;
            }

            key = node.Key;
            return true;
        }

        public bool TryGetMaxKey(out TKey key)
        {
            var node = _root;

            if (node is null)
            {
                key = default;
                return false;
            }

            while (node.Right is not null)
            {
                node = node.Right;
            }

            key = node.Key;
            return true;
        }

        // BALANCING
        private static int HeightOf(Node? node) => node?.Height ?? 0;

        private static void UpdateHeight(Node node)
        {
            node.Height = 1 + Math.Max(HeightOf(node.Left), HeightOf(node.Right));
        }

        // Positive = left side is taller, Negative = right side is taller
        private static int BalanceOf(Node node) => HeightOf(node.Left) - HeightOf(node.Right);

        private Node Rebalance(Node node)
        {
            UpdateHeight(node);
            var balance = BalanceOf(node);

            // Left side too tall
            if (balance > 1)
            {
                if (BalanceOf(node.Left!) < 0)
                {
                    node.Left = RotateLeft(node.Left!);
                }

                return RotateRight(node);
            }

            // Right side too tall
            if (balance < -1)
            {
                if (BalanceOf(node.Right!) > 0)
                {
                    node.Right = RotateRight(node.Right!);
                }

                return RotateLeft(node);
            }
            
            return node;
        }

        private Node RotateRight(Node top)
        {
            var newTop = top.Left!;
            top.Left = newTop.Right;
            newTop.Right = top;

            UpdateHeight(top);
            UpdateHeight(newTop);

            Rotations++;
            return newTop;
        }

        private Node RotateLeft(Node top)
        {
            var newTop = top.Right!;
            top.Right = newTop.Left;
            newTop.Left = top;

            UpdateHeight(top);
            UpdateHeight(newTop);

            Rotations++;
            return newTop;
        }
    }
}
