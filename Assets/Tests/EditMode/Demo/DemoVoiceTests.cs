using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using GatekeepersProtocol.Game;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The demo line runner and table (voice pipe). EditMode on purpose: the
    /// runner is engine-free, so id/table drift, the preempt/queue rules, the Subtitle
    /// clock and the escape/continue contract all pin here with no scene. What this
    /// suite cannot see: the authored box, cursor state, and real key input — those are
    /// the PlayMode rows and the playthrough.</summary>
    public class DemoVoiceTests
    {
        [SetUp]
        public void SetUp()
        {
            DemoVoice.ResetForTest();
            DemoVoice.ViewAttached();
        }

        [TearDown]
        public void TearDown()
        {
            DemoLines.SuppressTableForTest(false);
            DemoVoice.ResetForTest();
        }

        [Test]
        public void RegistryIds_AreUniqueAndWellFormed()
        {
            var seen = new HashSet<string>();
            foreach (string id in DemoConversationIds.All)
            {
                Assert.IsFalse(string.IsNullOrEmpty(id), "empty id in the registry");
                Assert.IsTrue(id.StartsWith("demo."), id + " must carry the demo. prefix");
                Assert.IsTrue(seen.Add(id), "duplicate registry id " + id);
            }
        }

        [Test]
        public void EveryRegistryId_HasATableRow()
        {
            foreach (string id in DemoConversationIds.All)
            {
                Assert.IsTrue(DemoLines.TryGet(id, out DemoLine row), "no table row for " + id);
                Assert.IsFalse(string.IsNullOrEmpty(row.Speaker), id + " has no speaker");
                Assert.IsFalse(string.IsNullOrEmpty(row.Text), id + " has no copy");
            }
        }

        [Test]
        public void EveryTableRow_IsInTheRegistry()
        {
            var registry = new HashSet<string>(DemoConversationIds.All);
            var rows = DemoLines.All;
            for (int i = 0; i < rows.Count; i++)
                Assert.IsTrue(registry.Contains(rows[i].Id),
                    "table row " + rows[i].Id + " is not a registry id");
        }

        [Test]
        public void Play_Lock_GoesLiveAndLocksInput()
        {
            DemoVoice.Play(DemoConversationIds.Wake, DemoVoiceMode.Lock);
            Assert.IsTrue(DemoVoice.IsLive);
            Assert.IsTrue(DemoVoice.LocksInput);
            Assert.AreEqual(DemoConversationIds.Wake, DemoVoice.CurrentId);
            Assert.AreEqual("Game", DemoVoice.CurrentSpeaker);
            Assert.IsFalse(string.IsNullOrEmpty(DemoVoice.CurrentText));
        }

        [Test]
        public void Continue_EndsTheLine()
        {
            DemoVoice.Play(DemoConversationIds.Wake, DemoVoiceMode.Lock);
            DemoVoice.Continue();
            Assert.IsFalse(DemoVoice.IsLive);
            Assert.IsFalse(DemoVoice.LocksInput);
        }

        [Test]
        public void Subtitle_DoesNotLockInput_AndAutoAdvances()
        {
            DemoVoice.Play(DemoConversationIds.HatchFight, DemoVoiceMode.Subtitle);
            Assert.IsTrue(DemoVoice.IsLive);
            Assert.IsFalse(DemoVoice.LocksInput, "a Subtitle must leave fight input live");

            DemoVoice.Tick(DemoVoice.SubtitleSeconds - 0.1f);
            Assert.IsTrue(DemoVoice.IsLive, "still inside the dwell");
            DemoVoice.Tick(0.2f);
            Assert.IsFalse(DemoVoice.IsLive, "auto-advance at the dwell");
        }

        [Test]
        public void UnknownId_NoOps()
        {
            DemoVoice.Play("demo.not_a_line", DemoVoiceMode.Lock);
            Assert.IsFalse(DemoVoice.IsLive);
            Assert.IsFalse(DemoVoice.LocksInput);
        }

        [Test]
        public void MissingTable_LogsOnceAndContinues()
        {
            DemoLines.SuppressTableForTest(true);
            Assert.IsFalse(DemoVoice.TableMissingLogged);
            DemoVoice.Play(DemoConversationIds.Wake, DemoVoiceMode.Lock);
            Assert.IsTrue(DemoVoice.TableMissingLogged, "first miss arms the once-guard");
            Assert.IsFalse(DemoVoice.IsLive, "mute, not stuck");
            DemoVoice.Play(DemoConversationIds.Wake, DemoVoiceMode.Lock);
            Assert.IsFalse(DemoVoice.IsLive);
        }

        [Test]
        public void NoViewInScene_DropsTheLine_NotStuck()
        {
            DemoVoice.ResetForTest(); // no ViewAttached
            DemoVoice.Play(DemoConversationIds.Wake, DemoVoiceMode.Lock);
            Assert.IsFalse(DemoVoice.IsLive);
            Assert.IsFalse(DemoVoice.LocksInput, "an invisible line must never eat input");
        }

        [Test]
        public void Play_WhileLive_Preempts()
        {
            DemoVoice.Play(DemoConversationIds.Wake, DemoVoiceMode.Lock);
            DemoVoice.Play(DemoConversationIds.NavRepair, DemoVoiceMode.Lock);
            Assert.AreEqual(DemoConversationIds.NavRepair, DemoVoice.CurrentId);
            Assert.IsNull(DemoVoice.QueuedId, "preempt never queues");
        }

        [Test]
        public void SameFrame_FirstLossLineWins_OtherQueues()
        {
            DemoVoice.Play(DemoConversationIds.FirstHatchDown, DemoVoiceMode.Lock);
            DemoVoice.Play(DemoConversationIds.CrewReady, DemoVoiceMode.Lock);
            Assert.AreEqual(DemoConversationIds.FirstHatchDown, DemoVoice.CurrentId);
            Assert.AreEqual(DemoConversationIds.CrewReady, DemoVoice.QueuedId);

            DemoVoice.Continue();
            Assert.AreEqual(DemoConversationIds.CrewReady, DemoVoice.CurrentId,
                "the queued line starts on Continue");
            Assert.IsNull(DemoVoice.QueuedId);
        }

        [Test]
        public void SameFrame_CollisionIsOrderIndependent()
        {
            DemoVoice.Play(DemoConversationIds.CrewReady, DemoVoiceMode.Lock);
            DemoVoice.Play(DemoConversationIds.FirstHatchDown, DemoVoiceMode.Lock);
            Assert.AreEqual(DemoConversationIds.FirstHatchDown, DemoVoice.CurrentId,
                "the first-loss line wins the frame even arriving second");
            Assert.AreEqual(DemoConversationIds.CrewReady, DemoVoice.QueuedId);
        }

        [Test]
        public void SameFrame_SubtitleQueuesBehindALiveLock()
        {
            DemoVoice.Play(DemoConversationIds.DoorD1, DemoVoiceMode.Lock);
            DemoVoice.Play(DemoConversationIds.DockOption, DemoVoiceMode.Subtitle);

            Assert.AreEqual(DemoConversationIds.DoorD1, DemoVoice.CurrentId,
                "the door line holds the frame it was raised on");
            Assert.AreEqual(DemoConversationIds.DockOption, DemoVoice.QueuedId,
                "the offer it sets up follows it instead of replacing it");

            DemoVoice.Continue();
            Assert.AreEqual(DemoConversationIds.DockOption, DemoVoice.CurrentId);
            Assert.IsFalse(DemoVoice.LocksInput, "the dock offer never eats input");
        }

        [Test]
        public void LaterFrame_SubtitleStillPreemptsALock()
        {
            DemoVoice.Play(DemoConversationIds.DoorD1, DemoVoiceMode.Lock);
            DemoVoice.Tick(0.016f);
            DemoVoice.Play(DemoConversationIds.DockOption, DemoVoiceMode.Subtitle);

            Assert.AreEqual(DemoConversationIds.DockOption, DemoVoice.CurrentId);
            Assert.IsNull(DemoVoice.QueuedId,
                "the follow-on rule is the same-frame pairing only");
        }

        [Test]
        public void LaterFrame_FirstLossLine_PreemptsNormally()
        {
            DemoVoice.Play(DemoConversationIds.CrewReady, DemoVoiceMode.Lock);
            DemoVoice.Tick(0.016f); // frame boundary — the collision window closes
            DemoVoice.Play(DemoConversationIds.FirstHatchDown, DemoVoiceMode.Lock);
            Assert.AreEqual(DemoConversationIds.FirstHatchDown, DemoVoice.CurrentId);
            Assert.IsNull(DemoVoice.QueuedId, "outside the same frame every collision preempts");
        }

        [Test]
        public void Escape_OnLock_EndsSameAsContinue()
        {
            DemoVoice.Play(DemoConversationIds.FirstHatchDown, DemoVoiceMode.Lock);
            DemoVoice.Play(DemoConversationIds.CrewReady, DemoVoiceMode.Lock);
            Assert.IsTrue(DemoVoice.TryHandleEscape(), "a live Lock line consumes Esc");
            Assert.AreEqual(DemoConversationIds.CrewReady, DemoVoice.CurrentId,
                "Esc advances into the queue exactly like Continue");
        }

        [Test]
        public void Escape_OnSubtitle_DismissesOnly()
        {
            DemoVoice.Play(DemoConversationIds.HatchFight, DemoVoiceMode.Subtitle);
            Assert.IsTrue(DemoVoice.TryHandleEscape(), "a live Subtitle consumes Esc");
            Assert.IsFalse(DemoVoice.IsLive);
        }

        [Test]
        public void Escape_WithNoLine_IsNotConsumed()
        {
            Assert.IsFalse(DemoVoice.TryHandleEscape(),
                "no live line — the scene dispatcher owns Esc");
        }

        [Test]
        public void TextOverride_ReplacesTableCopy()
        {
            DemoVoice.Play(DemoConversationIds.EmbarkBlocked, DemoVoiceMode.Lock,
                "You are unarmed. Take the MK-1 from the ARMORY.");
            Assert.AreEqual("You are unarmed. Take the MK-1 from the ARMORY.",
                DemoVoice.CurrentText);
        }

        [Test]
        public void Broadcast_FiresOnPlayLocalAndBroadcast_NotOnPlay()
        {
            var sent = new List<string>();
            DemoVoice.SetBroadcaster((id, mode) => sent.Add(id));

            DemoVoice.Play(DemoConversationIds.Wake, DemoVoiceMode.Lock);
            Assert.AreEqual(0, sent.Count, "a plain Play never broadcasts");

            DemoVoice.PlayLocalAndBroadcast(DemoConversationIds.DoorD1, DemoVoiceMode.Lock);
            Assert.AreEqual(1, sent.Count);
            Assert.AreEqual(DemoConversationIds.DoorD1, sent[0]);
            Assert.AreEqual(DemoConversationIds.DoorD1, DemoVoice.CurrentId,
                "the host seat plays locally too");
        }

        [Test]
        public void ViewDetached_EndsLiveAndQueued()
        {
            DemoVoice.Play(DemoConversationIds.FirstHatchDown, DemoVoiceMode.Lock);
            DemoVoice.Play(DemoConversationIds.CrewReady, DemoVoiceMode.Lock);
            DemoVoice.ViewDetached();
            Assert.IsFalse(DemoVoice.IsLive, "a line must not outlive its scene");
            Assert.IsNull(DemoVoice.QueuedId);
            Assert.IsFalse(DemoVoice.LocksInput);
        }

        [Test]
        public void StartLogPayload_MatchesTheRegistryFormat()
        {
            Assert.IsTrue(DemoLines.TryGet(DemoConversationIds.Wake, out DemoLine row));
            Assert.AreEqual(
                "line id=demo.wake mode=Lock scope=Local speaker=Game",
                DemoVoice.StartLogPayload(row, DemoVoiceMode.Lock));
        }

        [Test]
        public void LineRow_HasNoReplyField()
        {
            Assert.IsNull(typeof(DemoLine).GetField("Reply",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                "a reply field would be a player-talks-back surface");
        }

        /// <summary>The demo's beat-spec folder, found by STRUCTURE rather than by name: it
        /// is the one directory under Docs/ that holds voice.md, the line registry. The
        /// folder has been renamed once already and a hardcoded name turned that rename into
        /// a red test that said nothing about the code. Fails loudly when the folder is
        /// missing or ambiguous rather than falling back to a default — a default would turn
        /// a deleted spec into a passing run.</summary>
        static string BeatDocsFolder()
        {
            string docsRoot = Path.GetFullPath("Docs");
            Assert.IsTrue(Directory.Exists(docsRoot), "missing " + docsRoot);
            string found = null;
            foreach (string dir in Directory.GetDirectories(docsRoot))
            {
                if (!File.Exists(Path.Combine(dir, "voice.md"))) continue;
                Assert.IsNull(found,
                    "two folders under Docs/ hold voice.md (" + found + ", " + dir
                    + ") — the beat specs must have one home");
                found = dir;
            }
            Assert.IsNotNull(found,
                "no folder under Docs/ holds voice.md — the demo beat specs moved or were deleted");
            return found;
        }

        [Test]
        public void BeatDocs_CitedIdsAreInTheRegistry()
        {
            var registry = new HashSet<string>(DemoConversationIds.All);
            var dead = new HashSet<string> { "demo.door_d3" };
            string docs = BeatDocsFolder();
            foreach (string file in Directory.GetFiles(docs, "*.md"))
            {
                string text = File.ReadAllText(file);
                foreach (Match m in Regex.Matches(text, @"demo\.[a-z0-9_]+"))
                {
                    if (dead.Contains(m.Value)) continue;
                    Assert.IsTrue(registry.Contains(m.Value),
                        Path.GetFileName(file) + " cites " + m.Value);
                }
            }
        }

        [Test]
        public void ShipAndBastion_AuthorTheVoiceView()
        {
            string meta = File.ReadAllText(
                Path.GetFullPath("Assets/Scripts/Game/Ui/DemoVoiceView.cs.meta"));
            Match guid = Regex.Match(meta, @"^guid: ([0-9a-f]+)\s*$", RegexOptions.Multiline);
            Assert.IsTrue(guid.Success, "DemoVoiceView.cs.meta has no guid");
            string needle = "guid: " + guid.Groups[1].Value;
            Assert.Greater(Regex.Matches(
                File.ReadAllText(Path.GetFullPath("Assets/Scenes/Ship - CoOp.unity")), needle).Count, 0,
                "Ship.unity has no DemoVoiceView");
            Assert.Greater(Regex.Matches(
                File.ReadAllText(Path.GetFullPath("Assets/Scenes/Bastion_Coop.unity")), needle).Count, 0,
                "Bastion.unity has no DemoVoiceView");
        }
    }
}
