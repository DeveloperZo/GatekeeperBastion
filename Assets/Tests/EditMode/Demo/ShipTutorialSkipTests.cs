using GatekeepersProtocol.Game;
using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The ship tutorial is an arc — a lesson in where the stations are, how an
    /// interact works, and what firing feels like. Its flags live on the realm, so without a
    /// skip every new world replays a lesson its owner already had. These pin what the skip
    /// must do, what it must NOT do, and that it agrees with the gate it lifts.</summary>
    public class ShipTutorialSkipTests
    {
        [Test]
        public void AFreshRealm_IsHeldAtTheShipTerminal()
        {
            var m = MetaState.CreateNew();
            Assert.IsTrue(m.DemoActive, "a fresh realm starts inside the demo");
            Assert.AreEqual(DemoEmbarkRefusal.ShipTerminal, DemoProgress.EmbarkRefusal(m),
                "the first gate is the ship terminal");
        }

        [Test]
        public void Skipping_LiftsEveryGateTheTutorialOwns()
        {
            var m = MetaState.CreateNew();
            Assert.IsTrue(DemoProgress.SkipShipTutorial(m), "there was a tutorial to skip");

            Assert.AreEqual(DemoEmbarkRefusal.None, DemoProgress.EmbarkRefusal(m),
                "after the skip nothing may refuse the embark");
            Assert.IsTrue(m.DemoCapsuleOpened);
            Assert.IsTrue(m.DemoNavPowered);
            Assert.IsTrue(m.DemoArmoryPowered);
            Assert.IsTrue(m.DemoWeaponGranted);
            Assert.IsTrue(m.DemoShipFightDone);
        }

        [Test]
        public void Skipping_StillArmsThePlayer()
        {
            var m = MetaState.CreateNew();
            DemoProgress.SkipShipTutorial(m);

            Assert.IsTrue(m.Owns(WeaponId.Mk1Sidearm),
                "the tutorial's terminal hands over the MK-1; skipping the lesson must not "
                + "skip the equipment or the player embarks with the improvised cutter");
            Assert.AreEqual(WeaponId.Mk1Sidearm, m.EquippedSecondary,
                "and it must be in hand, not merely owned");
        }

        /// <summary>The lesson is beat 0. The zones and the rack that opens
        /// on zone 2's loss are the GAME — a skip that swallowed those would hand the player
        /// a finished demo instead of a skipped tutorial.</summary>
        [Test]
        public void Skipping_LeavesTheRestOfTheDemoAlone()
        {
            var m = MetaState.CreateNew();
            DemoProgress.SkipShipTutorial(m);

            Assert.IsTrue(m.DemoActive, "the demo itself is not over — only its first beat");
            Assert.IsFalse(m.DemoZone2BankLost, "zone 2 has still not been played");
            Assert.IsFalse(DemoProgress.PrimaryRowsOpen(m),
                "the rack still opens on Salvage earned, not on skipping the lesson");
            Assert.AreEqual(0, m.CompletedTerminalIds.Length, "no bastion progress was invented");
            Assert.AreEqual(0L, m.BankedSalvage, "and no salvage");
        }

        [Test]
        public void Skipping_IsIdempotent_AndRefusesWhenThereIsNothingToSkip()
        {
            var m = MetaState.CreateNew();
            Assert.IsTrue(DemoProgress.SkipShipTutorial(m));
            Assert.IsFalse(DemoProgress.SkipShipTutorial(m),
                "a second skip has nothing left to do");

            var off = MetaState.CreateNew();
            off.DemoActive = false;
            Assert.IsFalse(DemoProgress.SkipShipTutorial(off),
                "a session outside the demo has no tutorial to skip");

            Assert.IsFalse(DemoProgress.SkipShipTutorial(null), "and null is not a session");
        }

        /// <summary>The preference that carries the lesson between worlds. It defaults OFF so
        /// a first-time player is still taught — the whole design depends on that default,
        /// because the flag is switched on automatically and can never be switched back by
        /// anything except the player.</summary>
        [Test]
        public void TheSkipPreference_DefaultsOff_SoAFirstPlayerIsStillTaught()
        {
            Settings.Unbind(); // no store: every read answers the declared default
            Assert.IsFalse(Settings.GetBool(SettingId.SkipShipTutorial),
                "a machine that has never finished the tutorial must still be taught it");

            SettingDecl decl = Settings.DeclarationOf(SettingId.SkipShipTutorial);
            Assert.IsNotNull(decl, "the preference must be declared to be readable");
            Assert.AreEqual(SettingCategory.Gameplay, decl.Category,
                "it belongs with the other things a player chooses about play");
            Assert.IsFalse(string.IsNullOrEmpty(decl.PrefKey),
                "it must persist, or the lesson is re-taught every launch");
        }

        /// <summary>The completed tutorial writes the preference, and opening any later realm
        /// reads it. These are the two halves that make the lesson consumed once per PLAYER
        /// while its flags stay per-realm.</summary>
        [Test]
        public void TheSkipPreference_TurnsOn_AndThenSkipsAFreshRealm()
        {
            Settings.Unbind();
            Settings.SetBool(SettingId.SkipShipTutorial, true);
            Assert.IsTrue(Settings.GetBool(SettingId.SkipShipTutorial),
                "finishing the ship fight sets this");

            // What GameSession.Adopt does with it on the next world.
            var next = MetaState.CreateNew();
            Assert.AreEqual(DemoEmbarkRefusal.ShipTerminal, DemoProgress.EmbarkRefusal(next),
                "the fresh realm still starts gated — the realm knows nothing");
            if (Settings.GetBool(SettingId.SkipShipTutorial))
                DemoProgress.SkipShipTutorial(next);
            Assert.AreEqual(DemoEmbarkRefusal.None, DemoProgress.EmbarkRefusal(next),
                "but a player who has already learned it opens straight into the world");

            Settings.SetBool(SettingId.SkipShipTutorial, false);
            Settings.Unbind();
        }

        /// <summary>The dev checkpoints' beat 0 and the player's skip are the same operation.
        /// They were two copies of the same five flags until this was shared; a test that
        /// only checked one of them is how they would drift apart again.</summary>
        [Test]
        public void TheDevCheckpoint_UsesTheSameSkip()
        {
            string src = System.IO.File.ReadAllText(System.IO.Path.GetFullPath(
                "Assets/Scripts/Game/Bastion/GameController.DevCheckpoints.cs"));
            StringAssert.Contains("DemoProgress.SkipShipTutorial", src,
                "ApplyBeatZero must defer to the shared skip rather than re-listing the flags");
        }
    }
}
