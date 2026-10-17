namespace Perpetuum.Zones.Node
{
    /// <summary>
    /// A graph node with its associated edges.
    /// </summary>
    public sealed class RouteNode
    {
        /// <summary>
        /// Unique node identifier.
        /// </summary>
        /// <remarks>Initialized when the object is created and remains unchanged thereafter (init-only).</remarks>
        public int Id { get; init; }

        /// <summary>
        /// X-coordinate
        /// </summary>
        /// <remarks>The coordinate step is one tile. Initialized when the object is created and remains unchanged thereafter (init-only).</remarks>
        public int X { get; init; }
        /// <summary>
        /// Y-coordinate
        /// </summary>
        /// <remarks>The coordinate step is one tile. Initialized when the object is created and remains unchanged thereafter (init-only).</remarks>    
        public int Y { get; init; }

        /// <summary>
        /// A list of neighboring nodes.
        /// </summary>
        public List<RouteNode> Neighbors { get; } = new();
    }

    /// <summary>
    /// A graph composed of route nodes and edges.
    /// </summary>
    public sealed class RouteGraph
    {
        /// <summary>
        /// A dictionary of all nodes.
        /// </summary>
        public Dictionary<int, RouteNode> Nodes { get; } = new();
    }
}
