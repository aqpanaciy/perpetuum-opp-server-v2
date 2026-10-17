using Perpetuum.ExportedTypes;
using Perpetuum.Reactive;
using Perpetuum.StateMachines;
using Perpetuum.Threading.Process;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.NpcSystem.AI;
using System;

namespace Perpetuum.Services.RouteNode
{
    public class RouteNodeService : Process
    {
        private readonly IZone _zone;

        public RouteNodeService(IZone zone)
        {
            _zone = zone ?? throw new ArgumentNullException(nameof(zone));
        }

        public override void Update(TimeSpan time)
        {
            if (_zone.Players.IsNullOrEmpty() || _zone.RouteGraph == null)
            {
                return;
            }

            var player = _zone.Players.RandomElement();
            double distance = int.MaxValue;
            var nearestNode = 0;

            _zone.RouteGraph.Nodes.Values.ForEach(node =>
            {
                var dist = player.CurrentPosition.TotalDistance2D(node.X, node.Y);

                if (dist < distance)
                {
                    distance = dist;
                    nearestNode = node.Id;
                }
            });

            _zone.RouteGraph.Nodes.TryGetValue(nearestNode, out var node);
            var builder = Beam.NewBuilder().WithType(BeamType.red_20sec)
                .WithPosition((new Position(node.X, node.Y)).Center)
                .WithState(BeamState.AlignToTerrain)
                .WithDuration(20000);
            _zone.CreateBeam(builder);

            node.Neighbors.ForEach(neighbor =>
            {
                var position = new Position(node.X, node.Y).Center;
                var direction = position.DirectionTo(new Position(neighbor.X, neighbor.Y));
                while (position.TotalDistance2D(neighbor.X, neighbor.Y) > 1)
                {
                    position = position.OffsetInDirection(direction, 1);
                    var builder = Beam.NewBuilder().WithType(BeamType.green_20sec)
                        .WithPosition(position)
                        .WithState(BeamState.AlignToTerrain)
                        .WithDuration(20000);
                    _zone.CreateBeam(builder);
                }
            });
        }
    }
}
