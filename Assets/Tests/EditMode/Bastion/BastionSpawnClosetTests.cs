using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Spawn-closet wiring, pinned from scene YAML against the sketch document.
    /// The start-state rule is structural, not a hardcoded id list: a closet is inactive
    /// exactly when one owner will wake it (a locked door's sealedSpawns or the master
    /// terminal's unlockedSpawns), a closet is active exactly when nothing owns it, and
    /// every closet that is live at the start must sit in a room the player can reach
    /// with every gate shut. Seat mutes (`S:id:edge:3`) are runtime state, so only the
    /// serialized minSeats is pinned here.</summary>
    public class BastionSpawnClosetTests
    {
        const string CoopSketch = "Tools/bastion-sketch/bastion-coop-layout.txt";
        const string CoopScene = "Assets/Scenes/Bastion_Coop.unity";
        const string SpawnPointMeta = "Assets/Scripts/Game/Enemies/SpawnPoint.cs.meta";

        [Test]
        public void CoopClosets_MatchTheSketch_AndStartInTheRightState()
        {
            BastionSketch sketch = ParseSketch(CoopSketch);
            var marks = new List<SpawnMark>();
            string hatchRoom = null;
            for (int i = 0; i < sketch.Rooms.Count; i++)
            {
                BastionSketchRoom room = sketch.Rooms[i];
                for (int m = 0; m < room.Marks.Count; m++)
                {
                    BastionSketchMark mark = room.Marks[m];
                    if (mark.Kind == BastionSketchMarkKind.Spawn)
                        marks.Add(new SpawnMark
                        {
                            Id = mark.RefId,
                            RoomId = room.Id,
                            MinSeats = mark.MinSeats < 1 ? 1 : mark.MinSeats,
                        });
                    if (mark.Kind == BastionSketchMarkKind.Hatch && hatchRoom == null)
                        hatchRoom = room.Id;
                }
            }
            Assert.Greater(marks.Count, 0, CoopSketch + " declares no spawn closets");
            Assert.IsNotNull(hatchRoom, CoopSketch + " has no hatch room");

            SceneSpawns scene = SceneSpawns.Load(CoopScene, SpawnGuid());

            // One closet per sketch mark, named S_<id>, no strays (an S_xx is an unbound
            // closet the emitter never named).
            var sketchIds = new List<string>();
            for (int i = 0; i < marks.Count; i++)
                sketchIds.Add(marks[i].Id);
            sketchIds.Sort(System.StringComparer.Ordinal);
            var sceneIds = new List<string>(scene.ByName.Keys);
            sceneIds.Sort(System.StringComparer.Ordinal);
            Assert.AreEqual(string.Join(",", sketchIds), string.Join(",", sceneIds),
                CoopScene + " spawn points do not match the sketch's spawn marks");

            // Serialized minSeats mirrors the sketch's :n suffix.
            for (int i = 0; i < marks.Count; i++)
            {
                SpawnMark mark = marks[i];
                SceneSpawn spawn = scene.ByName[mark.Id];
                Assert.AreEqual(mark.MinSeats, spawn.MinSeats,
                    "S_" + mark.Id + " minSeats drifted from the sketch's " + mark.MinSeats);
            }

            AssertOwnershipMatchesStartState(scene, CoopScene);

            // Live-at-start closets must be fightable: their host room reachable with
            // every gate shut (enemies breaching an unreachable wing cannot be answered).
            Dictionary<string, List<string>> open = BastionSketchReach.BuildOpenEdges(sketch);
            HashSet<string> reach = BastionSketchReach.Flood(
                hatchRoom, open, new List<BastionSketchReach.LockedEdge>(), new HashSet<string>());
            for (int i = 0; i < marks.Count; i++)
            {
                SpawnMark mark = marks[i];
                if (scene.ByName[mark.Id].Owners != 0)
                    continue;
                Assert.IsTrue(reach.Contains(mark.RoomId),
                    "S_" + mark.Id + " is live at the start but its room " + mark.RoomId
                    + " is not reachable while every gate is shut");
            }
        }

        /// <summary>The two unlock LISTS exist and are non-empty, read straight off the YAML.
        /// The test above proves the same wiring the long way round, per closet; this one says
        /// it in one line, so a wholesale loss of the seal or master list reads as its own
        /// failure rather than as a scatter of per-closet ones.</summary>
        [Test]
        public void CoopScene_KeepsItsSealAndMasterWiring()
        {
            string yaml = File.ReadAllText(Path.GetFullPath(CoopScene)).Replace("\r\n", "\n");
            Match seal = Regex.Match(yaml,
                @"^  doorId: D2\n[\s\S]*?^  sealedSpawns:((?:\n  - \{fileID: \d+\})*)",
                RegexOptions.Multiline);
            Assert.IsTrue(seal.Success, CoopScene + " has no D2 door document");
            Assert.Greater(Regex.Matches(seal.Groups[1].Value, @"fileID: [1-9]").Count, 0,
                CoopScene + " D2 seals nothing — the locked-wing wiring is gone");
            Match master = Regex.Match(yaml,
                @"^  terminalId: Master\n[\s\S]*?^  unlockedSpawns:((?:\n  - \{fileID: \d+\})*)",
                RegexOptions.Multiline);
            Assert.IsTrue(master.Success, CoopScene + " has no master terminal document");
            Assert.Greater(Regex.Matches(master.Groups[1].Value, @"fileID: [1-9]").Count, 0,
                CoopScene + " master unlocks nothing — the control fight wiring is gone");
        }

        static void AssertOwnershipMatchesStartState(SceneSpawns scene, string scenePath)
        {
            for (int i = 0; i < scene.All.Count; i++)
            {
                SceneSpawn spawn = scene.All[i];
                if (spawn.Owners == 0)
                    Assert.IsTrue(spawn.Active,
                        scenePath + ": " + spawn.Name + " has no owner to wake it but starts "
                        + "inactive — it can never spawn");
                else
                {
                    Assert.AreEqual(1, spawn.Owners,
                        scenePath + ": " + spawn.Name + " is owned by " + spawn.Owners
                        + " unlockers — the second one re-wakes a wing out of order");
                    Assert.IsFalse(spawn.Active,
                        scenePath + ": " + spawn.Name + " is sealed behind an unlocker but "
                        + "starts active — the wing spawns before its gate opens");
                }
            }
        }

        static string SpawnGuid()
        {
            string meta = Path.GetFullPath(SpawnPointMeta);
            Assert.IsTrue(File.Exists(meta), "missing " + meta);
            Match m = Regex.Match(File.ReadAllText(meta), @"^guid: ([0-9a-f]+)\s*$", RegexOptions.Multiline);
            Assert.IsTrue(m.Success, SpawnPointMeta + " has no guid");
            return m.Groups[1].Value;
        }

        static BastionSketch ParseSketch(string relative)
        {
            string path = Path.GetFullPath(relative);
            Assert.IsTrue(File.Exists(path), "missing " + path);
            BastionSketchParseResult result = BastionSketchParser.Parse(File.ReadAllText(path));
            Assert.IsTrue(result.Success, relative + " does not parse: " + result.Error);
            return result.Sketch;
        }

        struct SpawnMark
        {
            public string Id;
            public string RoomId;
            public int MinSeats;
        }

        sealed class SceneSpawn
        {
            public string Name;
            public bool Active;
            public int Owners;
            public int MinSeats = 1;
        }

        /// <summary>Scene YAML index of every SpawnPoint component: its GameObject's name and
        /// active flag, how many unlockers own it (door sealedSpawns + master unlockedSpawns),
        /// and the owning SpawnRoom's serialized minSeats where one exists.</summary>
        sealed class SceneSpawns
        {
            public readonly List<SceneSpawn> All = new List<SceneSpawn>();
            public readonly Dictionary<string, SceneSpawn> ByName = new Dictionary<string, SceneSpawn>();
            public int SealedCount;
            public int MasterCount;

            public static SceneSpawns Load(string relative, string spawnGuid)
            {
                string path = Path.GetFullPath(relative);
                Assert.IsTrue(File.Exists(path), "missing " + path);
                string yaml = File.ReadAllText(path).Replace("\r\n", "\n");

                var classById = new Dictionary<string, string>();
                var goDocs = new Dictionary<string, string>();
                var monoDocs = new Dictionary<string, string>();
                MatchCollection heads = Regex.Matches(yaml, @"--- !u!(\d+) &(\d+)\n");
                var bodies = new Dictionary<string, string>();
                for (int i = 0; i < heads.Count; i++)
                {
                    string id = heads[i].Groups[2].Value;
                    int from = heads[i].Index + heads[i].Length;
                    int to = i + 1 < heads.Count ? heads[i + 1].Index : yaml.Length;
                    string body = yaml.Substring(from, to - from);
                    classById[id] = heads[i].Groups[1].Value;
                    bodies[id] = body;
                    if (heads[i].Groups[1].Value == "1")
                        goDocs[id] = body;
                    else if (heads[i].Groups[1].Value == "114")
                        monoDocs[id] = body;
                }

                var result = new SceneSpawns();
                var byCompId = new Dictionary<string, SceneSpawn>();
                foreach (KeyValuePair<string, string> mono in monoDocs)
                {
                    if (!mono.Value.Contains("guid: " + spawnGuid + ","))
                        continue;
                    Match goRef = Regex.Match(mono.Value, @"m_GameObject: \{fileID: (\d+)\}");
                    Assert.IsTrue(goRef.Success && goDocs.ContainsKey(goRef.Groups[1].Value),
                        relative + ": SpawnPoint component " + mono.Key + " has no GameObject");
                    string goDoc = goDocs[goRef.Groups[1].Value];
                    Match name = Regex.Match(goDoc, @"^  m_Name: (.+?)$", RegexOptions.Multiline);
                    Match active = Regex.Match(goDoc, @"^  m_IsActive: (\d)$", RegexOptions.Multiline);
                    var spawn = new SceneSpawn
                    {
                        Name = name.Success ? name.Groups[1].Value.Trim() : "?",
                        Active = active.Success && active.Groups[1].Value == "1",
                    };
                    result.All.Add(spawn);
                    byCompId[mono.Key] = spawn;
                    if (spawn.Name.StartsWith("S_", System.StringComparison.Ordinal))
                        result.ByName[spawn.Name.Substring(2)] = spawn;
                }

                foreach (KeyValuePair<string, string> mono in monoDocs)
                {
                    Match doorSeal = Regex.Match(mono.Value,
                        @"^  doorId: \S+\n[\s\S]*?^  sealedSpawns:((?:\n  - \{fileID: \d+\})*)",
                        RegexOptions.Multiline);
                    if (doorSeal.Success)
                    {
                        foreach (Match r in Regex.Matches(doorSeal.Groups[1].Value, @"fileID: (\d+)"))
                        {
                            if (byCompId.TryGetValue(r.Groups[1].Value, out SceneSpawn s))
                            {
                                s.Owners++;
                                result.SealedCount++;
                            }
                        }
                    }
                    Match master = Regex.Match(mono.Value,
                        @"^  terminalId: Master\n[\s\S]*?^  unlockedSpawns:((?:\n  - \{fileID: \d+\})*)",
                        RegexOptions.Multiline);
                    if (master.Success)
                    {
                        foreach (Match r in Regex.Matches(master.Groups[1].Value, @"fileID: (\d+)"))
                        {
                            if (byCompId.TryGetValue(r.Groups[1].Value, out SceneSpawn s))
                            {
                                s.Owners++;
                                result.MasterCount++;
                            }
                        }
                    }
                    Match room = Regex.Match(mono.Value,
                        @"^  minSeats: (\d+)", RegexOptions.Multiline);
                    if (room.Success)
                    {
                        Match sp = Regex.Match(mono.Value, @"spawnPoint: \{fileID: (\d+)\}");
                        if (sp.Success && byCompId.TryGetValue(sp.Groups[1].Value, out SceneSpawn s))
                            s.MinSeats = int.Parse(room.Groups[1].Value);
                    }
                }
                return result;
            }
        }
    }
}
