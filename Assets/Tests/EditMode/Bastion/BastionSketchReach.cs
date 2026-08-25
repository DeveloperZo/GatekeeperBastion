using System.Collections.Generic;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Room-graph reachability over a parsed sketch, shared by the mission and
    /// spawn-closet tests. Adjacency is read from footprints via <see cref="BastionKit"/>,
    /// so everything here runs offline.</summary>
    internal static class BastionSketchReach
    {
        internal struct LockedEdge
        {
            public BastionSketchDoor Door;
            public string RoomA;
            public string RoomB;
        }

        /// <summary>Open passages and stairs as an adjacency list over room ids.</summary>
        internal static Dictionary<string, List<string>> BuildOpenEdges(BastionSketch sketch)
        {
            var edges = new Dictionary<string, List<string>>();
            for (int i = 0; i < sketch.Rooms.Count; i++)
                edges[sketch.Rooms[i].Id] = new List<string>();

            List<BastionSketchPassage> passages = BastionSketchPassages.Find(sketch);
            for (int i = 0; i < passages.Count; i++)
            {
                BastionSketchPassage p = passages[i];
                string partner = PartnerAcross(sketch, p.RoomId, p.Direction, p.Along);
                Assert.IsNotNull(partner,
                    "passage " + p.Id + " on " + p.RoomId + " " + p.Direction
                    + " pairs with no room — the adjacency reading disagrees with the pairing");
                edges[p.RoomId].Add(partner);
                edges[partner].Add(p.RoomId);
            }

            for (int i = 0; i < sketch.Stairs.Count; i++)
            {
                BastionSketchStair s = sketch.Stairs[i];
                edges[s.LowerId].Add(s.UpperId);
                edges[s.UpperId].Add(s.LowerId);
            }
            return edges;
        }

        /// <summary>The room whose opposite footprint edge carries the aperture point —
        /// the far side of a passage or gate. Footprints only; no scene needed.</summary>
        internal static string PartnerAcross(
            BastionSketch sketch,
            string roomId,
            BastionSketchDirection direction,
            float along)
        {
            const float Eps = 0.05f;
            BastionSketchRoom room = null;
            for (int i = 0; i < sketch.Rooms.Count; i++)
            {
                if (sketch.Rooms[i].Id == roomId)
                    room = sketch.Rooms[i];
            }
            if (room == null)
                return null;

            float halfW = BastionKit.FootprintWidth(room.Kind) * 0.5f;
            float halfD = BastionKit.FootprintDepth(room.Kind) * 0.5f;
            bool northSouth = direction == BastionSketchDirection.North
                || direction == BastionSketchDirection.South;
            float edge = northSouth
                ? room.CenterZ + (direction == BastionSketchDirection.North ? halfD : -halfD)
                : room.CenterX + (direction == BastionSketchDirection.East ? halfW : -halfW);
            float at = northSouth ? room.CenterX + along : room.CenterZ + along;

            for (int i = 0; i < sketch.Rooms.Count; i++)
            {
                BastionSketchRoom other = sketch.Rooms[i];
                if (other.Id == roomId || other.Storey != room.Storey)
                    continue;
                float oHalfW = BastionKit.FootprintWidth(other.Kind) * 0.5f;
                float oHalfD = BastionKit.FootprintDepth(other.Kind) * 0.5f;
                if (northSouth)
                {
                    float oEdge = other.CenterZ
                        + (direction == BastionSketchDirection.North ? -oHalfD : oHalfD);
                    if (System.Math.Abs(oEdge - edge) < Eps
                        && System.Math.Abs(at - other.CenterX) <= oHalfW + Eps)
                        return other.Id;
                }
                else
                {
                    float oEdge = other.CenterX
                        + (direction == BastionSketchDirection.East ? -oHalfW : oHalfW);
                    if (System.Math.Abs(oEdge - edge) < Eps
                        && System.Math.Abs(at - other.CenterZ) <= oHalfD + Eps)
                        return other.Id;
                }
            }
            return null;
        }

        /// <summary>Rooms reachable from <paramref name="start"/> over open edges plus any
        /// locked edge whose door id is in <paramref name="openDoors"/>.</summary>
        internal static HashSet<string> Flood(
            string start,
            Dictionary<string, List<string>> open,
            List<LockedEdge> lockedEdges,
            HashSet<string> openDoors)
        {
            var reach = new HashSet<string>();
            var queue = new Queue<string>();
            reach.Add(start);
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                string room = queue.Dequeue();
                List<string> next = open[room];
                for (int i = 0; i < next.Count; i++)
                {
                    if (reach.Add(next[i]))
                        queue.Enqueue(next[i]);
                }
                for (int i = 0; i < lockedEdges.Count; i++)
                {
                    LockedEdge e = lockedEdges[i];
                    if (!openDoors.Contains(e.Door.DoorId))
                        continue;
                    string across = e.RoomA == room ? e.RoomB : (e.RoomB == room ? e.RoomA : null);
                    if (across != null && reach.Add(across))
                        queue.Enqueue(across);
                }
            }
            return reach;
        }
    }
}
