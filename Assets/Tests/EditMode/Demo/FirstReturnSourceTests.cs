using System.IO;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Source pins for M78's Game-side wiring — the first return to the Ship with
    /// salvage banked. These files are MonoBehaviours or GameSession-coupled statics the
    /// offline harness cannot run; what it can prove is that the wiring is present, ordered,
    /// and that no retired branch crept back. Behaviour is the editor's to confirm.</summary>
    public class FirstReturnSourceTests
    {
        static string Read(string relative)
        {
            string path = Path.GetFullPath(relative);
            Assert.IsTrue(File.Exists(path), path);
            return File.ReadAllText(path);
        }

        [Test]
        public void WheelCap_RefusesBeforeSpendSalvage_AndDecrementsOnSuccess()
        {
            string src = Read("Assets/Scripts/Game/Ship/ShipCommands.Weapons.cs");
            int buyStart = src.IndexOf("public static bool BuyWheelUpgrade", System.StringComparison.Ordinal);
            Assert.Greater(buyStart, -1, "the wheel command lives in the Weapons partial");
            int cap = src.IndexOf("wheel refused — demo cap", buyStart, System.StringComparison.Ordinal);
            int spend = src.IndexOf("m.SpendSalvage(price)", buyStart, System.StringComparison.Ordinal);
            Assert.Greater(cap, buyStart, "the cap refuse lives inside BuyWheelUpgrade");
            Assert.Less(cap, spend, "named cap refuse BEFORE any SpendSalvage (criterion 1)");
            StringAssert.Contains("m.DemoWheelBuysLeft--", src);
            StringAssert.Contains("demoBuysLeft=", src);
        }

        [Test]
        public void TheMk1_UnlocksMultiplayer_OncePerSaveByConstruction()
        {
            // Re-keyed 2026-08-25 (Zo): the ARMORY's MK-1 is the key, and `armingBeat` is
            // false once DemoWeaponGranted is set — so the line cannot fire twice.
            string src = Read("Assets/Scripts/Game/Ship/ShipCommands.Weapons.cs");
            StringAssert.Contains("!m.DemoWeaponGranted", src);
            StringAssert.Contains("\"multiplayer unlocked\"", src);
            StringAssert.Contains("DemoConversationIds.MpUnlocked", src);
            Assert.IsFalse(src.Contains("DemoWheelBuysLeft = 2"),
                "the count never resets per visit (beat 3, Traps)");
            Assert.IsFalse(Read("Assets/Scripts/Game/Ship/ShipCommands.cs")
                    .Contains("DemoWheelBuysLeft = 2"),
                "nor from the other command surface");
        }

        /// <summary>Rung 2 rides its own counter, refuses under its own name, and refuses
        /// before any spend (Docs/Demo - Single/5-repeat-to-control-room.md).</summary>
        [Test]
        public void RungTwo_RefusesBeforeSpendSalvage_OnItsOwnCounter()
        {
            string src = Read("Assets/Scripts/Game/Ship/ShipCommands.Weapons.cs");
            int buyStart = src.IndexOf("public static bool BuyWheelUpgrade", System.StringComparison.Ordinal);
            Assert.Greater(buyStart, -1, "the wheel command lives in the Weapons partial");
            int rung = src.IndexOf("wheel refused — ladder rung spent", buyStart,
                System.StringComparison.Ordinal);
            int spend = src.IndexOf("m.SpendSalvage(price)", buyStart, System.StringComparison.Ordinal);
            Assert.Greater(rung, buyStart, "the rung-2 refuse lives inside BuyWheelUpgrade");
            Assert.Less(rung, spend, "named rung refuse BEFORE any SpendSalvage");
            StringAssert.Contains("m.DemoRungTwoBuysLeft--", src, "rung 2 spends its own counter");
            StringAssert.Contains("m.DemoRungTwoOpened", src, "which name the refuse takes");
        }

        /// <summary>The Primary rows are a gate this milestone ADDS: refused named, before the
        /// price check and before any spend (Docs/Demo - Single/5-repeat-to-control-room.md).</summary>
        [Test]
        public void PrimaryBuy_RefusesBeforeSpendSalvage_UntilTheBankFalls()
        {
            string src = Read("Assets/Scripts/Game/Ship/ShipCommands.Weapons.cs");
            int buyStart = src.IndexOf("public static bool BuyWeapon", System.StringComparison.Ordinal);
            Assert.Greater(buyStart, -1, "the catalog buy lives in the Weapons partial");
            int refuse = src.IndexOf("weapon refused — primary locked", buyStart,
                System.StringComparison.Ordinal);
            int afford = src.IndexOf("m.BankedSalvage < cost", buyStart, System.StringComparison.Ordinal);
            int spend = src.IndexOf("m.SpendSalvage(cost)", buyStart, System.StringComparison.Ordinal);
            Assert.Greater(refuse, buyStart, "the primary gate lives inside BuyWeapon");
            Assert.Less(refuse, afford, "the lock is named before the price is");
            Assert.Less(refuse, spend, "named refuse BEFORE any SpendSalvage");
            StringAssert.Contains("DemoProgress.PrimaryRowsOpen", src);
            StringAssert.Contains("WeaponSlot.Primary", src);
        }

        /// <summary>The rack gate is a pure rule keyed on the bank loss, and the exit standing
        /// reads BOTH ladder counters — rung 1 alone would flip it to EMBARK while rung-2 buys
        /// are still affordable.</summary>
        [Test]
        public void PrimaryGate_IsPure_AndTheStandingKeyReadsBothRungs()
        {
            string src = Read("Assets/Scripts/Game/Demo/DemoProgress.cs");
            StringAssert.Contains("public static bool PrimaryRowsOpen", src);
            StringAssert.Contains("m.DemoZone2BankLost", src);
            Assert.IsFalse(src.Contains("PodEjections"),
                "the rack is keyed on the bank the player just lost, never on an ending count");
            Assert.IsFalse(src.Contains("UnityEngine"), "DemoProgress stays engine-free");
            StringAssert.Contains("m.DemoWheelBuysLeft + m.DemoRungTwoBuysLeft <= 0", src);
        }

        /// <summary>The rung-2 Docked resolve: line, log, counter, latch — and it runs after
        /// ResolveFirstReturn, which is the stated same-load order.</summary>
        [Test]
        public void RungTwoDocked_PlaysTheLine_LogsTheRung_AndLatchesOncePerSave()
        {
            string src = Read("Assets/Scripts/Game/Ship/DemoShipDirector.cs");
            int first = src.IndexOf("ResolveFirstReturn(m);", System.StringComparison.Ordinal);
            int rung = src.IndexOf("ResolveRungTwo(m);", System.StringComparison.Ordinal);
            Assert.Greater(first, -1);
            Assert.Greater(rung, first, "first-return resolves first on a same-load collision");
            StringAssert.Contains("DemoConversationIds.SecondLoss", src);
            StringAssert.Contains("\"ladder rung=2 opened — primary + 2 buys\"", src);
            StringAssert.Contains("m.DemoRungTwoBuysLeft = 2", src);
            StringAssert.Contains("m.DemoRungTwoOpened = true", src);
            // The ban is scoped to the RUNG-TWO resolve: that gate is DemoZone2BankLost and
            // never an ejection count. The FIRST-LOSS resolve in the same file legitimately
            // keys on PodEjections — its authored trigger IS the first Lost Boarding
            // (Docs/Demo - Single/2-first-hatch-bank.md), so a file-wide token ban would
            // outlaw the beat's own spec.
            int rungBody = src.IndexOf("void ResolveRungTwo", System.StringComparison.Ordinal);
            Assert.Greater(rungBody, -1);
            int rungEnd = src.IndexOf("private ", rungBody, System.StringComparison.Ordinal);
            if (rungEnd < 0) rungEnd = src.Length;
            string rungSrc = src.Substring(rungBody, rungEnd - rungBody);
            int close = rungSrc.IndexOf("\n        }\n", System.StringComparison.Ordinal);
            if (close > 0) rungSrc = rungSrc.Substring(0, close);
            Assert.IsFalse(rungSrc.Contains("PodEjections"),
                "the rung-two resolve is keyed on DemoZone2BankLost, never on an ejection count");
        }

        /// <summary>The restamped line must not assert a second ejection — this may be the
        /// save's first (Docs/Demo - Single/5-repeat-to-control-room.md, Open).</summary>
        [Test]
        public void SecondLossCopy_IsCauseAgnostic()
        {
            string src = Read("Assets/Scripts/Game/Demo/DemoLines.cs");
            StringAssert.Contains("DemoConversationIds.SecondLoss", src, "the id is kept");
            Assert.IsFalse(src.Contains("Thrown off again"),
                "the shipped line asserted a second ejection the recut forbids");
        }

        /// <summary>The PRIMARY tab stays reachable and says why it is empty; its BUY rows do
        /// not bind while the rack is sealed.</summary>
        [Test]
        public void ArmoryPrimaryTab_BindsNoBuyRows_ButStillSpeaks()
        {
            string src = Read("Assets/Scripts/Game/Ship/ArmoryStation.cs");
            StringAssert.Contains("DemoProgress.PrimaryRowsOpen", src);
            StringAssert.Contains("PrimarySealedLine", src);
            StringAssert.Contains("noBuyRows && !m.Owns(id)", src,
                "an owned Primary still binds — only the BUY rows are held back");
            StringAssert.Contains("_viewSlot = WeaponSlot.Primary", src,
                "the PRIMARY tab button stays bound");
            StringAssert.Contains("m.DemoRungTwoBuysLeft", src,
                "the panel rebinds when rung 2 hands out its buys");
        }

        [Test]
        public void ExitStanding_IsStateKeyed_AndReevaluatedOnEveryMoneyPath()
        {
            string ship = Read("Assets/Scripts/Game/Ship/DemoShipStanding.cs");
            StringAssert.Contains("NoBuyableUpgradeRemains", ship);
            StringAssert.Contains("exit standing set — no buyable upgrades remain", ship);
            string commands = Read("Assets/Scripts/Game/Ship/ShipCommands.cs")
                + Read("Assets/Scripts/Game/Ship/ShipCommands.Weapons.cs");
            int calls = 0;
            int at = 0;
            while ((at = commands.IndexOf("DemoShipStanding.Reevaluate()", at + 1,
                       System.StringComparison.Ordinal)) >= 0) calls++;
            Assert.GreaterOrEqual(calls, 5,
                "evaluated after every buy / refuse / spend (criterion 4)");
        }

        [Test]
        public void FirstDocked_PlaysTheLossLine_ThenCrew_ThenWritesTheUnlock()
        {
            string src = Read("Assets/Scripts/Game/Ship/DemoShipDirector.cs");
            int loss = src.IndexOf("DemoConversationIds.FirstHatchDown", System.StringComparison.Ordinal);
            int crew = src.IndexOf("DemoConversationIds.CrewReady", System.StringComparison.Ordinal);
            int flag = src.IndexOf("DemoCrewMenuUnlocked = true", System.StringComparison.Ordinal);
            Assert.Greater(loss, -1);
            Assert.Greater(crew, loss, "the first-loss line wins the frame; CREW queues");
            Assert.Greater(flag, crew, "the flag write is what makes both once-per-save");
            // Re-keyed 2026-08-25: the beat fires on the realm having earned anything, not on
            // a Convert window's counter — the Convert is out of the demo, so HatchConversions
            // can never rise and this branch would have been unreachable.
            StringAssert.Contains("LifetimeSalvageEarned > 0L", src);
            StringAssert.Contains("\"CREW menu unlocked\"", src);
        }

        [Test]
        public void CrewPanel_FoldsTheHealIn_AndNeverAutoEquips()
        {
            string src = Read("Assets/Scripts/Game/Ship/CrewQuartersStation.cs");
            StringAssert.Contains("DemoStationGate.HealOnVisit", src);
            StringAssert.Contains("DemoConversationIds.CrewClaim", src);
            Assert.IsFalse(src.Contains("TryClaim"),
                "the claim is the walk — BuyProtocol only ever fires from the sheet's EQUIP");
        }

        [Test]
        public void Nav_IsPlainEmbark_WhileTheDemoRuns()
        {
            string src = Read("Assets/Scripts/Game/Ship/MapStation.cs");
            StringAssert.Contains("_startWave = 1", src);
            StringAssert.Contains("DemoConversationIds.EmbarkWave2", src);
            StringAssert.Contains("ShowStartWavePicker(false)", src);
            StringAssert.Contains("lead && !counting && !m.DemoActive", src,
                "the party lead's picker is suppressed too");
        }

        [Test]
        public void Bastion_LogsThePlainEmbark()
        {
            string src = Read("Assets/Scripts/Game/Bastion/GameController.cs");
            StringAssert.Contains("\"embark plain\"", src);
            StringAssert.Contains("RecordCompositeClear()", src);
            StringAssert.Contains("ClearPendingRetrigger()", src);
        }

        [Test]
        public void Retrigger_LogsItsElection_ButNeverCompletesTheZone()
        {
            // Re-keyed 2026-08-26 (Zo; Docs/Design/0-Gameplay.md). This asserted that the
            // composite's clear logs "zone complete" — and that completion is exactly what
            // made the walk up the lane optional: killing the last body finished the zone from
            // wherever the player was standing. A wave ENDS AT A TERMINAL, so the clear now
            // records depth and nothing else, and GameController.ShutOffTerminal owns the
            // completion. The negative is the point of the test.
            string src = Read("Assets/Scripts/Game/Bastion/GameController.Retrigger.cs");
            StringAssert.Contains("\"zone retrigger zone=\"", src);
            StringAssert.Contains("mode=", src);
            StringAssert.Contains("TriggerCompositeDraw", src);
            StringAssert.Contains("DemoConversationIds.ZoneRetrigger", src);
            StringAssert.Contains("SetZoneDeepest", src);
            Assert.IsFalse(src.Contains("TryCompleteTerminal"),
                "the composite's clear completes a terminal again — surviving a Wave is not "
                + "the objective, reaching the terminal is");

            // ...and the completion lives with the shutoff, which is the verb that earns it.
            string owner = Read("Assets/Scripts/Game/Bastion/GameController.Terminals.cs");
            StringAssert.Contains("public bool ShutOffTerminal(", owner);
            StringAssert.Contains("OpenDoorsFor(\"shutoff\")", owner);
        }

        [Test]
        public void FieldClear_WritesThePerZoneDeepest()
        {
            string src = Read("Assets/Scripts/Game/Bastion/GameController.Terminals.cs");
            int clear = src.IndexOf("void RecordPendingTerminalClear", System.StringComparison.Ordinal);
            int write = src.IndexOf("SetZoneDeepest", clear, System.StringComparison.Ordinal);
            Assert.Greater(write, clear,
                "DemoZoneDeepestWave writes on field-Wave CLEAR, never on bank start");
            StringAssert.Contains("\"zone terminals complete zone=\"", src);
        }

        [Test]
        public void BankRefuse_NamesTheZone_NotTheTerminal()
        {
            string hatch = Read("Assets/Scripts/Game/Bastion/GameController.HatchBank.cs");
            StringAssert.Contains("\"bank refused — zone incomplete\"", hatch);
            Assert.IsFalse(hatch.Contains("finish the terminal"), "superseded refuse copy");
            StringAssert.Contains("\"bank window survived zone=\"", hatch);
            // The Hatch no longer REFUSES while the zone is unfinished — that is where the
            // wave now begins (Zo, 2026-08-25), so the prompt offers the start instead. The
            // sim-side refuse above is still the authority, and still names the zone.
            string repair = Read("Assets/Scripts/Game/Bastion/RepairInteractable.cs");
            StringAssert.Contains("start the wave", repair);
            Assert.IsFalse(repair.Contains("finish the terminal"), "superseded refuse copy");
        }

        [Test]
        public void ArmoryWheel_SpeaksTheCapLine_OncePerVisit()
        {
            string src = Read("Assets/Scripts/Game/Ship/ArmoryStation.cs");
            StringAssert.Contains("DemoConversationIds.WheelCap", src);
            StringAssert.Contains("_capLinePlayed", src);
            StringAssert.Contains("two purchases this demo — NAV is ready", src);
            Assert.IsFalse(src.Contains("survive a death first"),
                "the wheel lock copy follows the re-keyed gate");
        }
    }
}
