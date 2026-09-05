using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The Bastion holds its air: no face of any room opens on nothing.
    ///
    /// The doorways widened twice and one wall was removed outright on 2026-09-04, and each of
    /// those is a chance to leave a room open to space. The check is one rule per side kind
    /// against the COLLIDER layer, which is what actually contains a body — the pack meshes are
    /// dressing and hold no enclosure claim.
    ///
    /// The case worth naming is <c>None</c>. It means no wall is built at all, which is only
    /// honest when a neighbouring room covers that whole edge. A 42 m face whose neighbour is
    /// 16 m wide leaves 26 m of open space, and nothing in the geometry complains.
    ///
    /// YAML only, like the other scene pins: the offline harness cannot see MonoBehaviours. A
    /// missing scene is inconclusive rather than a fail.</summary>
    public class BastionEnclosureTests
    {
        const string ScenePath = "Assets/Scenes/Bastion_Coop.unity";
        const string SegmentGuid = "3f477662d2fd4221a148d00f85609434";

        /// <summary>Mirrors BastionSegment's constants, which are Game-layer and unreachable
        /// from this harness. BastionDoorApertureTests pins them against the real fields.</summary>
        const float DoorWidth = 10f;
        const float DividerWidth = 1.2f;
        const float Tolerance = 0.35f;

        /// <summary>Collider-layer pieces only. CreateBox emits these; WallPiece, Doorframe and
        /// WindowPiece are pack meshes from the visual layer.</summary>
        static readonly Regex ColliderPiece = new Regex(
            @"^Gen_(North|South|East|West)_(Wall|Door_West|Door_East|Door_Lintel|Window_\w+|Span_Divider)$");

        sealed class Node
        {
            public string Name = "";
            public string Parent = "0";
            public float[] Pos = { 0f, 0f, 0f };
            public float[] Scale = { 1f, 1f, 1f };
        }

        static float F(string s)
        {
            float v;
            float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
            return v;
        }

        [Test]
        public void EveryRoomFace_IsWalledOrOpensOnARoom()
        {
            string full = Path.GetFullPath(ScenePath);
            // A scene that is not on disk cannot be judged. The harness shim has no
            // Inconclusive, so this returns quietly; BastionCoopSceneTests is the pin that the
            // scene exists at all, and it fails loudly when it does not.
            if (!File.Exists(full))
                return;
            string yaml = File.ReadAllText(full);

            // fileID -> object name, and transform fileID -> node.
            var objectName = new Dictionary<string, string>();
            var nodes = new Dictionary<string, Node>();
            var kinds = new Dictionary<string, int[]>();   // room name -> N,E,S,W
            var blockKind = new Dictionary<string, int>();

            foreach (string doc in yaml.Split(new[] { "--- !u!" }, System.StringSplitOptions.None))
            {
                Match head = Regex.Match(doc, @"^(\d+) &(\d+)");
                if (!head.Success) continue;
                string cls = head.Groups[1].Value, id = head.Groups[2].Value;
                if (cls == "1")
                {
                    Match n = Regex.Match(doc, @"m_Name: (.*)");
                    objectName[id] = n.Success ? n.Groups[1].Value.Trim() : "";
                }
                else if (cls == "4")
                {
                    Match go = Regex.Match(doc, @"m_GameObject: \{fileID: (\d+)\}");
                    if (!go.Success) continue;
                    var node = new Node { Parent = "0" };
                    Match p = Regex.Match(doc, @"m_LocalPosition: \{x: ([-\d.eE+]+), y: ([-\d.eE+]+), z: ([-\d.eE+]+)\}");
                    if (p.Success) node.Pos = new[] { F(p.Groups[1].Value), F(p.Groups[2].Value), F(p.Groups[3].Value) };
                    Match s = Regex.Match(doc, @"m_LocalScale: \{x: ([-\d.eE+]+), y: ([-\d.eE+]+), z: ([-\d.eE+]+)\}");
                    if (s.Success) node.Scale = new[] { F(s.Groups[1].Value), F(s.Groups[2].Value), F(s.Groups[3].Value) };
                    Match f = Regex.Match(doc, @"m_Father: \{fileID: (\d+)\}");
                    if (f.Success) node.Parent = f.Groups[1].Value;
                    node.Name = go.Groups[1].Value;   // holds the GO id until resolved below
                    nodes[id] = node;
                }
                else if (cls == "114" && doc.Contains(SegmentGuid))
                {
                    Match go = Regex.Match(doc, @"m_GameObject: \{fileID: (\d+)\}");
                    if (!go.Success) continue;
                    var four = new int[4];
                    string[] keys = { "north", "east", "south", "west" };
                    for (int i = 0; i < 4; i++)
                    {
                        Match k = Regex.Match(doc, keys[i] + @": (\d+)");
                        four[i] = k.Success ? int.Parse(k.Groups[1].Value) : 0;
                    }
                    Match bk = Regex.Match(doc, @"blockKind: (\d+)");
                    kinds[go.Groups[1].Value] = four;
                    blockKind[go.Groups[1].Value] = bk.Success ? int.Parse(bk.Groups[1].Value) : 0;
                }
            }
            if (kinds.Count == 0)
                return;   // un-emitted copy; see the note above

            foreach (Node n in nodes.Values)
                n.Name = objectName.TryGetValue(n.Name, out string nm) ? nm : "?";

            // Footprints, by BastionBlockKind ordinal.
            float[][] foot =
            {
                new[] { 10f, 10f }, new[] { 16f, 16f }, new[] { 26f, 26f },
                new[] { 42f, 42f }, new[] { 94f, 47f },
            };

            var roomNode = new Dictionary<string, string>();
            foreach (var kv in nodes)
                if (Regex.IsMatch(kv.Value.Name, @"^R\d+$")) roomNode[kv.Value.Name] = kv.Key;

            float[] World(string id)
            {
                float x = 0f, y = 0f, z = 0f;
                int guard = 0;
                while (nodes.TryGetValue(id, out Node nd) && guard++ < 16)
                {
                    x += nd.Pos[0]; y += nd.Pos[1]; z += nd.Pos[2];
                    id = nd.Parent;
                }
                return new[] { x, y, z };
            }
            string RoomOf(string id)
            {
                int guard = 0;
                while (nodes.TryGetValue(id, out Node nd) && guard++ < 16)
                {
                    if (Regex.IsMatch(nd.Name, @"^R\d+$")) return nd.Name;
                    id = nd.Parent;
                }
                return null;
            }

            var pieces = new Dictionary<string, List<KeyValuePair<string, string>>>();
            foreach (var kv in nodes)
            {
                if (!ColliderPiece.IsMatch(kv.Value.Name)) continue;
                string room = RoomOf(kv.Key);
                if (room == null) continue;
                if (!pieces.TryGetValue(room, out var list))
                    pieces[room] = list = new List<KeyValuePair<string, string>>();
                list.Add(new KeyValuePair<string, string>(kv.Key, kv.Value.Name));
            }

            string[] edgeName = { "north", "east", "south", "west" };
            int[] edgeAxis = { 2, 0, 2, 0 };     // the axis the face sits on
            int[] edgeSign = { +1, +1, -1, -1 };
            int[] alongAxis = { 0, 2, 0, 2 };    // the axis the face runs along

            var holes = new List<string>();
            foreach (var kv in roomNode)
            {
                string room = kv.Key;
                string segGo = null;
                foreach (var g in nodes)
                    if (g.Key == kv.Value) segGo = g.Value.Name;
                // side kinds are keyed by the room's GameObject id
                int[] sides = null;
                foreach (var k in kinds)
                    if (objectName.TryGetValue(k.Key, out string n2) && n2 == room) { sides = k.Value; break; }
                int bk = 1;
                foreach (var k in blockKind)
                    if (objectName.TryGetValue(k.Key, out string n3) && n3 == room) { bk = k.Value; break; }
                if (sides == null) continue;

                float w = foot[bk][0], d = foot[bk][1];
                float[] rw = World(kv.Value);

                for (int e = 0; e < 4; e++)
                {
                    int kind = sides[e];
                    float span = alongAxis[e] == 0 ? w : d;
                    float half = edgeAxis[e] == 2 ? d * 0.5f : w * 0.5f;
                    float plane = rw[edgeAxis[e]] + edgeSign[e] * half;

                    var segs = new List<float[]>();
                    if (pieces.TryGetValue(room, out var list))
                    {
                        string prefix = "Gen_" + char.ToUpper(edgeName[e][0]) + edgeName[e].Substring(1) + "_";
                        foreach (var pr in list)
                        {
                            if (!pr.Value.StartsWith(prefix)) continue;
                            float[] pw = World(pr.Key);
                            if (System.Math.Abs(pw[edgeAxis[e]] - plane) > 1.5f) continue;
                            float c = pw[alongAxis[e]];
                            float L = System.Math.Abs(nodes[pr.Key].Scale[alongAxis[e]]);
                            segs.Add(new[] { c - L * 0.5f, c + L * 0.5f });
                        }
                    }
                    segs.Sort((a, b) => a[0].CompareTo(b[0]));
                    var merged = new List<float[]>();
                    foreach (float[] s in segs)
                    {
                        if (merged.Count > 0 && s[0] <= merged[merged.Count - 1][1] + 1e-3f)
                            merged[merged.Count - 1][1] = System.Math.Max(merged[merged.Count - 1][1], s[1]);
                        else merged.Add(new[] { s[0], s[1] });
                    }
                    float lo = rw[alongAxis[e]] - span * 0.5f, hi = rw[alongAxis[e]] + span * 0.5f;
                    float gap = 0f, cur = lo;
                    foreach (float[] m in merged)
                    {
                        if (m[0] > cur + 0.05f) gap += m[0] - cur;
                        cur = System.Math.Max(cur, m[1]);
                    }
                    if (hi > cur + 0.05f) gap += hi - cur;

                    string where = room + " " + edgeName[e];
                    switch (kind)
                    {
                        case 0:   // Wall
                            if (gap > Tolerance) holes.Add(where + ": Wall with " + gap.ToString("0.0") + " m open");
                            break;
                        case 1:   // Door
                            if (System.Math.Abs(gap - DoorWidth) > Tolerance)
                                holes.Add(where + ": Door opens " + gap.ToString("0.0") + " m, not " + DoorWidth);
                            break;
                        case 2:   // Window — sill, lintel and walk block seal the aperture
                            if (gap > Tolerance) holes.Add(where + ": Window with " + gap.ToString("0.0") + " m open");
                            break;
                        case 3:   // None — legal only where a neighbour covers the whole edge
                            holes.Add(where + ": side is None, so " + span.ToString("0.0")
                                + " m of wall is absent and only a neighbour of equal span could cover it");
                            break;
                        case 4:   // OpenSpan — the wall's absence bar the centre divider
                            if (System.Math.Abs(gap - (span - DividerWidth)) > Tolerance)
                                holes.Add(where + ": OpenSpan opens " + gap.ToString("0.0")
                                    + " m, not " + (span - DividerWidth).ToString("0.0"));
                            break;
                    }
                }
            }

            Assert.AreEqual(0, holes.Count,
                "the Bastion is open to space:\n  " + string.Join("\n  ", holes));
        }
    }
}
