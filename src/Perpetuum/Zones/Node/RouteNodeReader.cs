using Perpetuum.Data;
using Perpetuum.Log;
using System.Data;

namespace Perpetuum.Zones.Node
{
    /// <summary>
    /// A class for reading a graph from a database.
    /// </summary>
    public class RouteNodeReader
    {
        /// <summary>
        /// We load the graph from the database.
        /// </summary>
        /// <param name="zoneID">Zone identifier.</param>
        /// <returns>Loaded graph or NULL</returns>
        [CanBeNull]
        public RouteGraph GetAll(int zoneID)
        {
            var graph = new RouteGraph();

            // Read all the nodes of the graph and their coordinates.
            Db.Query().CommandText("SELECT * FROM dbo.NpcRouteNode WHERE ZoneId = @zoneID")
                .SetParameter("@zoneID", zoneID)
                .Execute()
                .Select(CreateRouteNodeFromRecord).ForEach(node => graph.Nodes.Add(node.Id, node));

            // If there are no nodes, return null to indicate that the graph is empty.
            if (graph.Nodes.Count == 0)
            {
                return null;
            }

            // Read all the edges of the graph and create the connections between the nodes.
            Db.Query().CommandText("SELECT * FROM dbo.NpcRouteEdge WHERE ZoneId = @zoneID")
                .SetParameter("@zoneID", zoneID)
                .Execute()
                .ForEach(record => CreateRouteEdgeFromRecord(record, graph));

            // Log the number of nodes and edges in the graph.
            var nodeCount = graph.Nodes.Count;
            var edgeCount = graph.Nodes.Values.Sum(n => n.Neighbors.Count) / 2;
            Logger.Info($"[RouteNodeReader] Loaded route graph for zone {zoneID}: {nodeCount} nodes, {edgeCount} edges.");

            return graph;
        }

        private RouteNode CreateRouteNodeFromRecord(IDataRecord record)
        {
            return new RouteNode
            {
                Id = record.GetValue<int>("Id"),
                X = record.GetValue<int>("X"),
                Y = record.GetValue<int>("Y")
            };
        }

        /// <summary>
        /// Connect the graph nodes with edges.
        /// </summary>
        /// <param name="record">A record from the database.</param>
        /// <param name="graph">A graph with nodes already loaded.</param>
        /// <exception cref="InvalidOperationException">The edge contains nodes that are not in the graph.</exception>
        private void CreateRouteEdgeFromRecord(IDataRecord record, RouteGraph graph)
        {
            var fromId = record.GetValue<int>("FromNodeId");
            var toId = record.GetValue<int>("ToNodeId");

            if (!graph.Nodes.TryGetValue(fromId, out var fromNode))
            {
                throw new InvalidOperationException($"Route node {fromId} not found.");
            }

            if (!graph.Nodes.TryGetValue(toId, out var toNode))
            {
                throw new InvalidOperationException($"Route node {toId} not found.");
            }

            fromNode.Neighbors.Add(toNode);
            toNode.Neighbors.Add(fromNode);
        }
    }
}
