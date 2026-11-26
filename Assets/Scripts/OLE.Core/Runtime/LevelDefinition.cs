using System;
using System.Collections.Generic;

namespace OLE.Core
{
    public enum BoardMode
    {
        A,
        B
    }

    // Connections are bidirectional, as in the diagrams in section 2.2.
    public sealed class Connection
    {
        public string First { get; }
        public string Second { get; }
        public bool OpenInA { get; }
        public bool OpenInB { get; }

        public Connection(string first, string second, bool openInA, bool openInB)
        {
            if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second))
                throw new ArgumentException("A connection needs two platform IDs.");
            if (first == second)
                throw new ArgumentException("A connection must join different platforms.");

            First = first;
            Second = second;
            OpenInA = openInA;
            OpenInB = openInB;
        }

        public bool IsOpen(BoardMode mode)
        {
            switch (mode)
            {
                case BoardMode.A: return OpenInA;
                case BoardMode.B: return OpenInB;
                default: throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

    }

    public sealed class LevelDefinition
    {
        private readonly HashSet<string> platformIds;
        private readonly Dictionary<string, List<Connection>> adjacent = new Dictionary<string, List<Connection>>(StringComparer.Ordinal);
        private readonly HashSet<string> reachable = new HashSet<string>(StringComparer.Ordinal);

        public string Id { get; }
        public string Start { get; }
        public string Goal { get; }
        public IReadOnlyList<string> Platforms { get; }
        public IReadOnlyList<Connection> Connections { get; }

        public LevelDefinition(string id, string start, string goal,
            IEnumerable<string> platforms, IEnumerable<Connection> connections)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("The level ID is required.", nameof(id));
            if (platforms == null) throw new ArgumentNullException(nameof(platforms));
            if (connections == null) throw new ArgumentNullException(nameof(connections));

            var orderedPlatforms = new List<string>(platforms);
            platformIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (string platform in orderedPlatforms)
            {
                if (string.IsNullOrWhiteSpace(platform) || !platformIds.Add(platform))
                    throw new ArgumentException("Platform IDs must be nonempty and unique.", nameof(platforms));
            }
            if (!platformIds.Contains(start) || !platformIds.Contains(goal))
                throw new ArgumentException("Start and goal must be platforms in this level.");

            foreach (string platform in orderedPlatforms) adjacent.Add(platform, new List<Connection>());
            var edges = new List<Connection>();
            var pairs = new HashSet<(string, string)>();
            foreach (Connection edge in connections)
            {
                if (edge == null || !platformIds.Contains(edge.First) || !platformIds.Contains(edge.Second))
                    throw new ArgumentException("A connection refers to an unknown platform.", nameof(connections));
                var pair = string.CompareOrdinal(edge.First, edge.Second) < 0
                    ? (edge.First, edge.Second) : (edge.Second, edge.First);
                if (!pairs.Add(pair))
                    throw new ArgumentException("Declare each connection once with both mode flags.", nameof(connections));
                edges.Add(edge);
                adjacent[edge.First].Add(edge);
                adjacent[edge.Second].Add(edge);
            }

            Id = id;
            Start = start;
            Goal = goal;
            Platforms = orderedPlatforms.AsReadOnly();
            Connections = edges.AsReadOnly();
            foreach (var state in RouteHints.Walk(this, Start, BoardMode.A)) reachable.Add(state.Platform);
            if (!reachable.Contains(Goal))
                throw new ArgumentException("The goal must be reachable from the start.", nameof(connections));
        }

        public bool HasPlatform(string id) => platformIds.Contains(id);

        internal bool CanRestoreAt(string id) => reachable.Contains(id);

        internal IEnumerable<string> OpenNeighbours(string from, BoardMode mode)
        {
            if (from == null || !adjacent.TryGetValue(from, out List<Connection> connections)) yield break;
            foreach (Connection edge in connections)
                if (edge.IsOpen(mode)) yield return edge.First == from ? edge.Second : edge.First;
        }

        public bool CanTraverse(string from, string to, BoardMode mode)
        {
            foreach (string neighbour in OpenNeighbours(from, mode))
                if (neighbour == to) return true;
            return false;
        }
    }
}
