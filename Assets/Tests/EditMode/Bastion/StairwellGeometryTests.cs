using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Walkability pins for every stair well the co-op sketch declares.
    /// The stair list is read from the sketch document, not hardcoded, so adding a
    /// stair there extends this check with no edit here. Geometry is parsed from the
    /// scene YAML (Force Text), so this runs offline as well as in the editor.
    /// The invariants are the fit pass's contract: the walk ramp's foot is buried
    /// below the lower floor so its end-cap is not a wall, its head reaches the
    /// upper storey, and a landing sits at the head with its top flush with the
    /// upper floor.</summary>
    public class StairwellGeometryTests
    {
        const string ScenePath = "Assets/Scenes/Bastion_Coop.unity";
        const string SketchPath = "Tools/bastion-sketch/bastion-coop-layout.txt";
        const string RoomsParentName = "Authored";

        [Test]
        public void EveryDeclaredStair_HasBuriedWalk_AndFlushLanding()
        {
            List<string[]> pairs = StairPairs();
            Assert.Greater(pairs.Count, 0, SketchPath + " declares no 'stair <lower> <upper>' lines");

            SceneDocs scene = SceneDocs.Load(ScenePath);

            for (int i = 0; i < pairs.Count; i++)
            {
                string lowerId = pairs[i][0];
                string upperId = pairs[i][1];
                string suffix = lowerId + "_" + upperId;

                V3 lowerPos = scene.WorldOf(scene.UniqueUnder(RoomsParentName, lowerId)).Pos;
                V3 upperPos = scene.WorldOf(scene.UniqueUnder(RoomsParentName, upperId)).Pos;
                double rise = upperPos.Y - lowerPos.Y;
                Assert.Greater(rise, 3.0, suffix + ": storeys are not stacked");
                Assert.Less(rise, 8.0, suffix + ": storey rise is implausible");

                string walkName = "GStairWalk_" + suffix;
                string landName = "GStairLand_" + suffix;
                Assert.IsTrue(scene.Has("Stair_" + suffix), "Stair_" + suffix + " missing from " + ScenePath);
                Assert.IsTrue(scene.Has(walkName), walkName + " missing from " + ScenePath);
                Assert.IsTrue(scene.Has(landName),
                    landName + " missing — the stair never got its fit pass, so there is no floor at the head");

                World walk = scene.WorldOf(scene.Unique(walkName));
                World land = scene.WorldOf(scene.Unique(landName));

                Assert.IsTrue(scene.HasBoxCollider(scene.Unique(walkName)), walkName + " has no BoxCollider");
                Assert.IsTrue(scene.HasBoxCollider(scene.Unique(landName)), landName + " has no BoxCollider");

                // The walk is a tilted box whose long axis is local +z. WHICH end is the
                // head depends on how the stair was turned, so take the higher one rather
                // than assuming a facing — a ramp flipped 180° is still a ramp.
                V3 axis = walk.Rot.Rotate(new V3(0, 0, 1));
                double halfLen = walk.Scale.Z * 0.5;
                V3 endA = new V3(
                    walk.Pos.X + axis.X * halfLen,
                    walk.Pos.Y + axis.Y * halfLen,
                    walk.Pos.Z + axis.Z * halfLen);
                V3 endB = new V3(
                    walk.Pos.X - axis.X * halfLen,
                    walk.Pos.Y - axis.Y * halfLen,
                    walk.Pos.Z - axis.Z * halfLen);
                V3 head = endA.Y >= endB.Y ? endA : endB;
                V3 foot = endA.Y >= endB.Y ? endB : endA;

                Assert.LessOrEqual(foot.Y, lowerPos.Y - 0.8,
                    walkName + ": foot ends at y=" + foot.Y.ToString("0.00")
                    + " — not buried below the lower floor, so its end-cap walls off the ramp");
                Assert.GreaterOrEqual(head.Y, upperPos.Y - 0.4,
                    walkName + ": head reaches only y=" + head.Y.ToString("0.00")
                    + " — short of the upper storey");
                Assert.GreaterOrEqual(walk.Scale.X, 2.8, walkName + " is narrower than the capsule needs");
                Assert.AreEqual(0.35, walk.Scale.Y, 0.05, walkName + " thickness drifted");

                double landTop = land.Pos.Y + land.Scale.Y * 0.5;
                Assert.AreEqual(upperPos.Y, landTop, 0.03,
                    landName + ": top at y=" + landTop.ToString("0.00")
                    + " is not flush with the upper floor at y=" + upperPos.Y.ToString("0.00"));
                Assert.GreaterOrEqual(land.Scale.X, 2.8, landName + " is narrower than the walk");
                Assert.GreaterOrEqual(land.Scale.Z, 1.5, landName + " is too shallow to step onto");

                // The landing adjoins the head of the climb rather than floating elsewhere.
                double dx = land.Pos.X - head.X;
                double dz = land.Pos.Z - head.Z;
                double gap = Math.Sqrt(dx * dx + dz * dz);
                Assert.LessOrEqual(gap, 2.0,
                    landName + " sits " + gap.ToString("0.00") + " m from the walk's head");

                // And the climb has to arrive at an OPENING. The fit pass punches the upper
                // storey's floor into slabs around the well; the head must fall in the gap
                // between them. A stair moved or turned without a re-punch climbs into the
                // underside of that floor — invisible to every check above, because the walk
                // and the landing stay perfectly consistent with each other while the hole
                // they need stays behind. Floor tiles are visual only; the collider is the floor.
                string upperGo = scene.UniqueUnder(RoomsParentName, upperId);
                List<string> subtree = scene.Subtree(upperGo);
                int solidFloors = 0;
                for (int s = 0; s < subtree.Count; s++)
                {
                    string solidId = subtree[s];
                    if (!scene.IsActive(solidId) || !scene.HasBoxCollider(solidId))
                        continue;
                    string solidName = scene.NameOf(solidId);
                    if (!solidName.StartsWith("Gen_Floor", StringComparison.Ordinal))
                        continue;

                    solidFloors++;
                    World slab = scene.WorldOf(solidId);
                    // The punched slabs are emitted axis-aligned, so extents are the box.
                    double halfX = Math.Abs(slab.Scale.X) * 0.5;
                    double halfZ = Math.Abs(slab.Scale.Z) * 0.5;
                    bool covers = head.X >= slab.Pos.X - halfX && head.X <= slab.Pos.X + halfX
                        && head.Z >= slab.Pos.Z - halfZ && head.Z <= slab.Pos.Z + halfZ;
                    Assert.IsFalse(covers,
                        suffix + ": the climb tops out at (" + head.X.ToString("0.00") + ", "
                        + head.Z.ToString("0.00") + ") under solid floor '" + solidName
                        + "' x[" + (slab.Pos.X - halfX).ToString("0.00") + ", "
                        + (slab.Pos.X + halfX).ToString("0.00") + "] z["
                        + (slab.Pos.Z - halfZ).ToString("0.00") + ", "
                        + (slab.Pos.Z + halfZ).ToString("0.00")
                        + "] — the well opening did not follow the stair, so the climb ends "
                        + "in the ceiling of " + lowerId);
                }
                Assert.Greater(solidFloors, 0,
                    upperId + " has no active floor collider at all — the opening check "
                    + "proved nothing");
            }
        }

        static List<string[]> StairPairs()
        {
            string path = Path.GetFullPath(SketchPath);
            Assert.IsTrue(File.Exists(path), "missing " + path);
            var pairs = new List<string[]>();
            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                Match m = Regex.Match(lines[i], @"^stair\s+(\S+)\s+(\S+)\s*$");
                if (m.Success)
                    pairs.Add(new[] { m.Groups[1].Value, m.Groups[2].Value });
            }
            return pairs;
        }

        struct V3
        {
            public double X, Y, Z;
            public V3(double x, double y, double z) { X = x; Y = y; Z = z; }
            public V3 Mul(V3 s) => new V3(X * s.X, Y * s.Y, Z * s.Z);
            public V3 Add(V3 o) => new V3(X + o.X, Y + o.Y, Z + o.Z);
        }

        struct Q4
        {
            public double X, Y, Z, W;
            public Q4(double x, double y, double z, double w) { X = x; Y = y; Z = z; W = w; }

            public Q4 Mul(Q4 o) => new Q4(
                W * o.X + X * o.W + Y * o.Z - Z * o.Y,
                W * o.Y + Y * o.W + Z * o.X - X * o.Z,
                W * o.Z + Z * o.W + X * o.Y - Y * o.X,
                W * o.W - X * o.X - Y * o.Y - Z * o.Z);

            public V3 Rotate(V3 v)
            {
                var c1 = Cross(X, Y, Z, v.X, v.Y, v.Z);
                var t = new V3(c1.X * 2, c1.Y * 2, c1.Z * 2);
                var c2 = Cross(X, Y, Z, t.X, t.Y, t.Z);
                return new V3(v.X + W * t.X + c2.X, v.Y + W * t.Y + c2.Y, v.Z + W * t.Z + c2.Z);
            }

            static V3 Cross(double ax, double ay, double az, double bx, double by, double bz)
                => new V3(ay * bz - az * by, az * bx - ax * bz, ax * by - ay * bx);
        }

        struct World
        {
            public V3 Pos;
            public Q4 Rot;
            public V3 Scale;
        }

        sealed class Node
        {
            public string GoId;
            public V3 Pos;
            public Q4 Rot;
            public V3 Scale;
            public string Father;
            public List<string> Children = new List<string>();
        }

        /// <summary>Scene YAML index: GameObjects by name, transforms by id, world-space
        /// composition up the father chain. Scale composes per-axis, which is exact here
        /// because every parent in a stair's chain is uniform-scaled; a rotated
        /// non-uniform parent would need a full matrix and fails loudly instead.</summary>
        sealed class SceneDocs
        {
            readonly Dictionary<string, string> _goDocsById = new Dictionary<string, string>();
            readonly Dictionary<string, Node> _nodesByTransformId = new Dictionary<string, Node>();
            readonly Dictionary<string, string> _transformIdByGoId = new Dictionary<string, string>();
            readonly Dictionary<string, List<string>> _goIdsByName = new Dictionary<string, List<string>>();
            readonly Dictionary<string, int> _classById = new Dictionary<string, int>();

            public static SceneDocs Load(string relative)
            {
                string path = Path.GetFullPath(relative);
                Assert.IsTrue(File.Exists(path), "missing " + path);
                var docs = new SceneDocs();
                docs.Parse(File.ReadAllText(path));
                return docs;
            }

            void Parse(string yaml)
            {
                MatchCollection heads = Regex.Matches(yaml, @"--- !u!(\d+) &(\d+)\r?\n");
                for (int i = 0; i < heads.Count; i++)
                {
                    int cls = int.Parse(heads[i].Groups[1].Value);
                    string id = heads[i].Groups[2].Value;
                    int from = heads[i].Index + heads[i].Length;
                    int to = i + 1 < heads.Count ? heads[i + 1].Index : yaml.Length;
                    string body = yaml.Substring(from, to - from);
                    _classById[id] = cls;

                    if (cls == 1)
                    {
                        _goDocsById[id] = body;
                        Match name = Regex.Match(body, @"^  m_Name: (.*?)\r?$", RegexOptions.Multiline);
                        if (name.Success)
                        {
                            string key = name.Groups[1].Value.Trim();
                            if (!_goIdsByName.TryGetValue(key, out List<string> list))
                            {
                                list = new List<string>();
                                _goIdsByName[key] = list;
                            }
                            list.Add(id);
                        }
                    }
                    else if (cls == 4)
                    {
                        var node = new Node
                        {
                            GoId = Field(body, @"m_GameObject: \{fileID: (\d+)\}"),
                            Pos = Vec(body, "m_LocalPosition"),
                            Rot = Quat(body),
                            Scale = Vec(body, "m_LocalScale"),
                            Father = Field(body, @"m_Father: \{fileID: (\d+)\}"),
                            Children = ChildIds(body)
                        };
                        _nodesByTransformId[id] = node;
                        if (node.GoId != null)
                            _transformIdByGoId[node.GoId] = id;
                    }
                }
            }

            public bool Has(string name) => _goIdsByName.ContainsKey(name);

            public string Unique(string name)
            {
                Assert.IsTrue(_goIdsByName.TryGetValue(name, out List<string> ids),
                    name + " not found in scene");
                Assert.AreEqual(1, ids.Count, name + " is ambiguous: " + ids.Count + " objects share it");
                return ids[0];
            }

            public string UniqueUnder(string parentName, string name)
            {
                Assert.IsTrue(_goIdsByName.TryGetValue(name, out List<string> ids),
                    name + " not found in scene");
                string found = null;
                for (int i = 0; i < ids.Count; i++)
                {
                    if (ParentNameOf(ids[i]) != parentName)
                        continue;
                    Assert.IsNull(found, name + " under " + parentName + " is ambiguous");
                    found = ids[i];
                }
                Assert.IsNotNull(found, name + " with parent " + parentName + " not found in scene");
                return found;
            }

            /// <summary>Every GameObject in this one's subtree, itself included.</summary>
            public List<string> Subtree(string goId)
            {
                var found = new List<string>();
                if (!_transformIdByGoId.TryGetValue(goId, out string root))
                    return found;
                var pending = new Stack<string>();
                pending.Push(root);
                while (pending.Count > 0)
                {
                    string tid = pending.Pop();
                    if (!_nodesByTransformId.TryGetValue(tid, out Node node))
                        continue;
                    if (node.GoId != null)
                        found.Add(node.GoId);
                    for (int i = 0; i < node.Children.Count; i++)
                        pending.Push(node.Children[i]);
                }
                return found;
            }

            /// <summary>The object's own active flag. Its ancestors are the room subtree
            /// the caller already walked from, so self-active is the whole answer here.</summary>
            public bool IsActive(string goId)
            {
                if (!_goDocsById.TryGetValue(goId, out string body))
                    return false;
                Match m = Regex.Match(body, @"^  m_IsActive: (\d)$", RegexOptions.Multiline);
                return !m.Success || m.Groups[1].Value == "1";
            }

            public string NameOf(string goId)
            {
                if (!_goDocsById.TryGetValue(goId, out string body))
                    return "?";
                Match m = Regex.Match(body, @"^  m_Name: (.*?)\r?$", RegexOptions.Multiline);
                return m.Success ? m.Groups[1].Value.Trim() : "?";
            }

            static List<string> ChildIds(string body)
            {
                var ids = new List<string>();
                Match block = Regex.Match(body, @"m_Children:((?:\r?\n  - \{fileID: \d+\})*)");
                if (!block.Success)
                    return ids;
                foreach (Match m in Regex.Matches(block.Groups[1].Value, @"fileID: (\d+)"))
                    ids.Add(m.Groups[1].Value);
                return ids;
            }

            public bool HasBoxCollider(string goId)
            {
                if (!_goDocsById.TryGetValue(goId, out string body))
                    return false;
                MatchCollection comps = Regex.Matches(body, @"component: \{fileID: (\d+)\}");
                for (int i = 0; i < comps.Count; i++)
                {
                    if (_classById.TryGetValue(comps[i].Groups[1].Value, out int cls) && cls == 65)
                        return true;
                }
                return false;
            }

            public World WorldOf(string goId)
            {
                Assert.IsTrue(_transformIdByGoId.TryGetValue(goId, out string tid),
                    "GameObject " + goId + " has no Transform document — a stripped or prefab-instance "
                    + "node is in the chain, which this parser cannot compose");
                return Compose(tid);
            }

            World Compose(string transformId)
            {
                Node node = _nodesByTransformId[transformId];
                if (node.Father == null || node.Father == "0")
                    return new World { Pos = node.Pos, Rot = node.Rot, Scale = node.Scale };

                Assert.IsTrue(_nodesByTransformId.ContainsKey(node.Father),
                    "transform " + node.Father + " missing from scene — chain broken");
                World p = Compose(node.Father);
                return new World
                {
                    Pos = p.Pos.Add(p.Rot.Rotate(node.Pos.Mul(p.Scale))),
                    Rot = p.Rot.Mul(node.Rot),
                    Scale = p.Scale.Mul(node.Scale)
                };
            }

            string ParentNameOf(string goId)
            {
                if (!_transformIdByGoId.TryGetValue(goId, out string tid))
                    return null;
                Node node = _nodesByTransformId[tid];
                if (node.Father == null || node.Father == "0")
                    return null;
                if (!_nodesByTransformId.TryGetValue(node.Father, out Node parent))
                    return null;
                if (parent.GoId == null || !_goDocsById.TryGetValue(parent.GoId, out string body))
                    return null;
                Match name = Regex.Match(body, @"^  m_Name: (.*?)\r?$", RegexOptions.Multiline);
                return name.Success ? name.Groups[1].Value.Trim() : null;
            }

            static string Field(string body, string pattern)
            {
                Match m = Regex.Match(body, pattern);
                return m.Success ? m.Groups[1].Value : null;
            }

            static V3 Vec(string body, string key)
            {
                Match m = Regex.Match(body,
                    key + @": \{x: ([-\d.eE+]+), y: ([-\d.eE+]+), z: ([-\d.eE+]+)\}");
                Assert.IsTrue(m.Success, key + " missing from transform document");
                return new V3(Num(m.Groups[1].Value), Num(m.Groups[2].Value), Num(m.Groups[3].Value));
            }

            static Q4 Quat(string body)
            {
                Match m = Regex.Match(body,
                    @"m_LocalRotation: \{x: ([-\d.eE+]+), y: ([-\d.eE+]+), z: ([-\d.eE+]+), w: ([-\d.eE+]+)\}");
                Assert.IsTrue(m.Success, "m_LocalRotation missing from transform document");
                return new Q4(Num(m.Groups[1].Value), Num(m.Groups[2].Value),
                    Num(m.Groups[3].Value), Num(m.Groups[4].Value));
            }

            static double Num(string s)
                => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
