using System;
using System.Reflection;
using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    public class BoarderProtocolTests
    {
        [Test]
        public void Catalog_DefaultSheet_Pins()
        {
            Assert.AreEqual(BoarderProtocolId.Depot, BoarderProtocol.ShopProtocols()[0]);
            Assert.AreEqual(3, BoarderProtocol.ShopProtocols().Length);
            Assert.AreEqual(DoctrineMode.Stock, BoarderProtocol.DoctrineOf(BoarderProtocolId.Depot));
            Assert.AreEqual(0, (int)DoctrineMode.Stock);
            Assert.AreEqual(BoarderAbilityId.Barrage, BoarderProtocol.CrewAbilityOf(BoarderProtocolId.Depot));
            Assert.AreEqual(BoarderAbilityId.Rush, BoarderProtocol.PersonalAbilityOf(BoarderProtocolId.Depot));
            Assert.AreEqual(0, BoarderProtocol.PassiveCount(BoarderProtocolId.Depot));
            Assert.AreEqual(BoarderAbilityId.None, BoarderProtocol.PassiveAt(BoarderProtocolId.Depot, 0));
            Assert.AreEqual(5, (int)BoarderAbilityId.Barrage);
            Assert.AreEqual(6, (int)BoarderAbilityId.Rush);
            Assert.AreEqual(8f, BoarderProtocol.Window(BoarderAbilityId.Barrage), 1e-5f);
            Assert.AreEqual(30f, BoarderProtocol.Cooldown(BoarderAbilityId.Barrage), 1e-5f);
            Assert.AreEqual(2f, BoarderProtocol.FireMul(BoarderAbilityId.Barrage), 1e-5f);
            Assert.AreEqual(8f, BoarderProtocol.Radius(BoarderAbilityId.Barrage), 1e-5f);
            // RUSH IS 6 s / 40 s and has been since M70 ("Personal H | Rush | ... | 6 s / 40 s").
            // NOT RE-KEYED ON 2026-08-28. The only window that changed that day is OVERCHARGE's
            // (6 -> 8, to match its new 40-round belt at the overcharged rate), and Overcharge
            // was folded into the switch arm Window() already shared with Bulkhead, Rush and
            // Cloak — so all four moved. Three of them were not meant to. Raising this pin to 8
            // would bless that, which is the whole reason it is written out rather than derived
            // from a sibling: a literal cannot drift along with the thing beside it.
            Assert.AreEqual(6f, BoarderProtocol.Window(BoarderAbilityId.Rush), 1e-5f,
                "Rush's window moved with Overcharge's - they share a case arm in "
                + "BoarderProtocol.Window and only Overcharge was meant to go 6 -> 8");
            Assert.AreEqual(6f, BoarderProtocol.Window(BoarderAbilityId.Bulkhead), 1e-5f,
                "and Bulkhead moved with it");
            Assert.AreEqual(6f, BoarderProtocol.Window(BoarderAbilityId.Cloak), 1e-5f,
                "and Cloak moved with it");
            Assert.AreEqual(8f, BoarderProtocol.Window(BoarderAbilityId.Overcharge), 1e-5f,
                "Overcharge is the one window that was meant to move");
            Assert.AreEqual(40f, BoarderProtocol.Cooldown(BoarderAbilityId.Rush), 1e-5f);
            Assert.AreEqual(1f, BoarderProtocol.FireMul(BoarderAbilityId.Rush), 1e-5f,
                "Feed (Rush id) regenerates clip — fire mul stays 1");
            Assert.AreEqual(0.22f, BoarderProtocol.FeedRegenOfFireRate, 1e-5f);
            Assert.AreEqual(0.35f, BoarderProtocol.DumpMoveMul, 1e-5f);
            Assert.AreEqual(2.2f, BoarderProtocol.FeedRegenPerSecond(10f), 1e-5f);
            Assert.Less(BoarderProtocol.FeedRegenPerSecond(10f), 10f);
            Assert.AreEqual("DEPOT Protocol", BoarderProtocol.ProtocolName(BoarderProtocolId.Depot));
            Assert.AreEqual("Dump", BoarderProtocol.DisplayName(BoarderAbilityId.Barrage));
            Assert.AreEqual("Feed", BoarderProtocol.DisplayName(BoarderAbilityId.Rush));
            Assert.AreEqual(0.5f, BoarderProtocol.Magnitude(BoarderAbilityId.Anchor), 1e-5f);
            Assert.AreEqual(0.70f, BoarderProtocol.Magnitude(BoarderAbilityId.Ghost), 1e-5f);
            Assert.IsTrue(BoarderProtocol.IsLegalPair(BoarderProtocolId.Depot, BoarderAbilityId.None));
            Assert.IsFalse(BoarderProtocol.IsLegalPair(BoarderProtocolId.Depot, BoarderAbilityId.Ghost));
            Assert.IsFalse(BoarderProtocol.IsLegalPair(BoarderProtocolId.None, BoarderAbilityId.None));
            Assert.AreEqual(BoarderProtocol.CrewSheetDefault, BoarderProtocol.CrewSheet(BoarderProtocolId.Depot));
            Assert.AreEqual(BoarderProtocol.PersonalSheetDefault, BoarderProtocol.PersonalSheet(BoarderProtocolId.Depot));
            Assert.AreEqual(BoarderProtocol.PassiveSheetDefault, BoarderProtocol.PassiveSheet(BoarderProtocolId.Depot));
            Assert.AreEqual("", BoarderProtocol.CrewSheet(BoarderProtocolId.None));
            var sheet = BoarderProtocol.SheetOf(BoarderProtocolId.Depot);
            Assert.AreEqual(BoarderProtocol.ProtocolName(BoarderProtocolId.Depot), sheet.Name);
            Assert.AreEqual(BoarderProtocol.DoctrineText(BoarderProtocolId.Depot), sheet.Doctrine);
            Assert.AreEqual(BoarderProtocol.CrewSheetDefault, sheet.Crew);
            Assert.AreEqual(BoarderProtocol.PersonalSheetDefault, sheet.Personal);
            Assert.AreEqual(BoarderProtocol.PassiveSheetDefault, sheet.Passive);
        }

        [Test]
        public void OverlayFill_ReadyIsZero_WindowAndCooldownAreFractions()
        {
            Assert.AreEqual(0f, BoarderProtocol.OverlayFill(0f, 0f, 6f, 40f), 1e-5f);
            Assert.AreEqual(0f, BoarderProtocol.OverlayFill(0.04f, 0.04f, 6f, 40f), 1e-5f);
            Assert.AreEqual(0.5f, BoarderProtocol.OverlayFill(3f, 0f, 6f, 40f), 1e-5f);
            Assert.AreEqual(0.5f, BoarderProtocol.OverlayFill(0f, 20f, 6f, 40f), 1e-5f);
            Assert.AreEqual(1f, BoarderProtocol.OverlayFill(8f, 0f, 8f, 30f), 1e-5f);
            Assert.AreEqual(0.5f, BoarderProtocol.OverlayFill(4f, 15f, 8f, 30f), 1e-5f);
        }

        [Test]
        public void ComingSoonNames_AreNotCatalogIds()
        {
            // VEIL now parses — it is a sold protocol. AEGIS and LEDGER are still only copy,
            // and the catalog must not answer to either.
            BoarderProtocolId aegis, ledger;
            Assert.IsFalse(Enum.TryParse("AEGIS", true, out aegis));
            Assert.IsFalse(Enum.TryParse("LEDGER", true, out ledger));
            var shop = BoarderProtocol.ShopProtocols();
            for (int i = 0; i < shop.Length; i++)
            {
                string name = BoarderProtocol.ProtocolName(shop[i]);
                Assert.AreNotEqual("AEGIS", name);
                Assert.AreNotEqual("LEDGER", name);
                Assert.IsNotEmpty(name);
            }
        }

        [Test]
        public void ComposeFireMul_SetThenProduct()
        {
            // Feed no longer contributes a self fire mul — rushOpen is ignored.
            Assert.AreEqual(2f, BoarderProtocol.ComposeFireMul(false, true, 1f), 1e-5f);
            Assert.AreEqual(1f, BoarderProtocol.ComposeFireMul(true, false, 1f), 1e-5f);
            Assert.AreEqual(2f, BoarderProtocol.ComposeFireMul(true, true, 2f), 1e-5f);
            Assert.AreEqual(2f, BoarderProtocol.ComposeFireMul(false, false, 2f), 1e-5f);
            Assert.AreEqual(2f, BoarderProtocol.ComposeFireMul(false, true, 2f), 1e-5f);
            Assert.AreEqual(2f, BoarderProtocol.ComposeFireMul(false, false, 4f), 1e-5f);
            Assert.AreEqual(1f, BoarderProtocol.ComposeFireMul(false, false, 1f), 1e-5f);
        }

        [Test]
        public void AbilityState_ActivateTickExpire_ThenCooldown()
        {
            var s = new BoarderAbilityState();
            Assert.IsTrue(s.IsReady);
            Assert.IsTrue(s.TryActivate(6f, 40f));
            Assert.IsTrue(s.WindowOpen);
            Assert.IsFalse(s.TryActivate(6f, 40f), "no refresh while open");
            s.Tick(6f);
            Assert.IsFalse(s.WindowOpen);
            Assert.Greater(s.CooldownRemaining, 0f);
            s.Tick(40f);
            Assert.IsTrue(s.IsReady);
        }

        [Test]
        public void AbilityState_EndWindowEarly_StartsCooldown_SecondsSatReadyOnActivate()
        {
            var s = new BoarderAbilityState();
            s.Tick(3.5f);
            Assert.AreEqual(3.5f, s.SecondsSatReady, 1e-4f);
            Assert.IsTrue(s.TryActivate(8f, 30f));
            Assert.AreEqual(0f, s.SecondsSatReady, 1e-5f);
            s.EndWindowEarly();
            Assert.IsFalse(s.WindowOpen);
            Assert.AreEqual(30f, s.CooldownRemaining, 1e-5f);
            s.EndWindowEarly();
            Assert.AreEqual(30f, s.CooldownRemaining, 1e-5f);
        }

        [Test]
        /// <summary>Re-keyed 2026-09-04: a fresh Boarder carries the GRANT, not None. The
        /// old rule was None-until-claim, which let a Boarder embark with no crew ability, no
        /// personal ability and no doctrine.</summary>
        public void FreshSave_CarriesTheGrant_AndAnotherClaimReplacesIt()
        {
            var m = MetaState.CreateNew();
            Assert.AreEqual(BoarderProtocol.GrantedProtocol, m.EquippedProtocol,
                "a fresh Boarder embarks carrying the granted protocol");
            Assert.AreEqual(BoarderProtocolId.Depot, m.EquippedProtocol);
            Assert.AreEqual(BoarderAbilityId.None, m.EquippedPassive,
                "the grant owns no passive, so nothing else moves with it");
            // v9 landed with the demo flags; the pin follows the schema, not a literal.
            Assert.AreEqual(SaveFormat.CurrentVersion, m.SaveVersion);
            // Claiming the thing you were granted is refused as already owned, which is what
            // the CREW row reads to print EQUIPPED instead of a claim button.
            Assert.IsFalse(BoarderProtocol.TryClaim(m, BoarderProtocolId.Depot));
            Assert.IsFalse(BoarderProtocol.TryClaim(m, BoarderProtocolId.None));
            // The grant is a floor, not a ceiling: another protocol still replaces it, and
            // arrives wearing its first passive.
            Assert.IsTrue(BoarderProtocol.TryClaim(m, BoarderProtocolId.Veil));
            Assert.AreEqual(BoarderProtocolId.Veil, m.EquippedProtocol);
            Assert.AreEqual(BoarderAbilityId.Ghost, m.EquippedPassive);
            var locked = MetaState.CreateNew();
            locked.PayloadLoadoutLocked = true;
            Assert.IsFalse(BoarderProtocol.TryClaim(locked, BoarderProtocolId.Veil));
        }

        [Test]
        public void PickPassive_Refuses_NoDefaultPassives()
        {
            var m = MetaState.CreateNew();
            Assert.IsFalse(m.SetEquippedPassive(BoarderAbilityId.Ghost));
            Assert.AreEqual(BoarderAbilityId.None, m.EquippedPassive);
            BoarderProtocol.TryClaim(m, BoarderProtocolId.Depot);
            Assert.IsFalse(m.SetEquippedPassive(BoarderAbilityId.Foresight));
            Assert.AreEqual(BoarderAbilityId.None, m.EquippedPassive);
        }

        [Test]
        public void LoadedDefaultWearingGhost_KeepsDefault_DropsThePassive()
        {
            // The equipped pair rides the Boarder, so this is a Boarder document.
            const string json =
                "{\"SaveVersion\":13,\"EquippedProtocol\":1,\"EquippedPassive\":3}";
            var m = BoarderState.FromJson(json);
            Assert.IsNotNull(m, "a current-version Boarder document loads");
            Assert.AreEqual(BoarderProtocolId.Depot, m.EquippedProtocol);
            Assert.AreEqual(BoarderAbilityId.None, m.EquippedPassive);
        }

        [Test]
        public void LoadedUncataloguedProtocol_TakesThePassiveDownWithIt()
        {
            const string json =
                "{\"SaveVersion\":13,\"EquippedProtocol\":99,\"EquippedPassive\":1}";
            var m = BoarderState.FromJson(json);
            Assert.IsNotNull(m, "a current-version Boarder document loads");
            // Re-keyed 2026-09-04: an id no switch has an arm for falls to the GRANT rather
            // than to None, so the load produces a Boarder who can fight.
            Assert.AreEqual(BoarderProtocol.GrantedProtocol, m.EquippedProtocol);
            Assert.AreEqual(BoarderAbilityId.None, m.EquippedPassive,
                "the borrowed passive goes down with the id it belonged to");
        }

        [Test]
        public void RemovedTracksAreGone()
        {
            var upgrades = typeof(MetaUpgrades);
            Assert.IsNull(upgrades.GetMethod("PlayerMaxHp",
                BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null));
            Assert.IsNotNull(upgrades.GetMethod("PlayerMaxHp",
                BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null));
            Assert.IsNull(typeof(MetaState).GetField("VitalityLevel"));
            Assert.IsNull(typeof(MetaState).GetField("StaminaLevel"));
            Assert.IsNull(typeof(MetaState).GetField("ScavengingLevel"));
            Assert.IsNull(typeof(MetaState).GetField("LuckLevel"));
            Assert.IsFalse(Enum.IsDefined(typeof(UpgradeTrack), "Vitality"));
            Assert.IsFalse(Enum.IsDefined(typeof(UpgradeTrack), "Stamina"));
            Assert.IsFalse(Enum.IsDefined(typeof(UpgradeTrack), "Scavenging"));
            Assert.IsFalse(Enum.IsDefined(typeof(UpgradeTrack), "Luck"));
        }

        [Test]
        public void Constants_AreLevelZeroValues()
        {
            Assert.AreEqual(100f, MetaUpgrades.PlayerMaxHp(), 1e-5f);
            Assert.AreEqual(100f, MetaUpgrades.StaminaMax(), 1e-5f);
            // 25 -> 42 on 2026-08-27. "Level zero" means no TRACK moves this, not that it never
            // retunes: the roster was cut ~40% and a kill is priced flat, so the price of a kill
            // had to rise by the same 5/3 or a run's income would have fallen with the bodies.
            Assert.AreEqual(42f, MetaUpgrades.SalvagePerKill(), 1e-5f);
            Assert.AreEqual(0f, MetaUpgrades.CritChance(), 1e-5f);
        }

        [Test]
        public void DecoyTryPlace_WritesStock_AndBaselineUnchanged()
        {
            var s = new DecoyState();
            s.ResetForRun(PayloadId.Decoy);
            float pull = s.PullRadius;
            float hp = s.MaxHp;
            int maxLive = s.MaxLive;
            Assert.IsTrue(s.TryPlace(1f, 0f, 2f, DoctrineMode.Stock, out _));
            Assert.AreEqual(DoctrineMode.Stock, s.Mode);
            Assert.AreEqual(pull, s.PullRadius, 1e-5f);
            Assert.AreEqual(hp, s.MaxHp, 1e-5f);
            Assert.AreEqual(maxLive, s.MaxLive);
            Assert.IsTrue(DecoyState.ShouldPull(GatekeeperKind.Default, false, true, 1f, pull));
        }

        /// <summary>The shop's purchase refuses while a Run holds the loadout lock. Pinned here
        /// because it is why the practice range does NOT call TryClaim: a range is Tethered from
        /// its first frame, so the lock is on for the whole life of the lane and every protocol
        /// cycle after the first came back "BuyProtocol refused — run in progress track=crew".
        /// RangeConsole.GrantProtocol sets EquippedProtocol directly instead. If this rule is
        /// ever relaxed, that workaround can go — but nobody should fold the range back onto
        /// TryClaim while the rule stands.</summary>
        [Test]
        public void TryClaim_RefusesUnderTheRunLoadoutLock()
        {
            MetaState m = MetaState.CreateNew();
            m.Boarder.PayloadLoadoutLocked = false;
            Assert.IsTrue(BoarderProtocol.TryClaim(m, BoarderProtocolId.Veil),
                "precondition: Veil is claimable when no run holds the loadout");

            MetaState locked = MetaState.CreateNew();
            locked.Boarder.PayloadLoadoutLocked = true;
            Assert.IsFalse(BoarderProtocol.TryClaim(locked, BoarderProtocolId.Veil),
                "the shop must still refuse a purchase mid-run");
            Assert.AreNotEqual(BoarderProtocolId.Veil, locked.EquippedProtocol,
                "a refused claim must not have changed the equipped protocol");
        }

        /// <summary>And the direct set the range uses works under that same lock, with the
        /// protocol's first passive seeded the way TryClaim seeds it.</summary>
        [Test]
        public void DirectSet_WorksUnderTheLock_AndSeedsTheFirstPassive()
        {
            MetaState m = MetaState.CreateNew();
            m.Boarder.PayloadLoadoutLocked = true;

            m.EquippedProtocol = BoarderProtocolId.Veil;
            m.EquippedPassive = BoarderProtocol.PassiveCount(BoarderProtocolId.Veil) > 0
                ? BoarderProtocol.PassiveAt(BoarderProtocolId.Veil, 0)
                : BoarderAbilityId.None;

            Assert.AreEqual(BoarderProtocolId.Veil, m.EquippedProtocol);
            if (BoarderProtocol.PassiveCount(BoarderProtocolId.Veil) > 0)
                Assert.AreEqual(BoarderProtocol.PassiveAt(BoarderProtocolId.Veil, 0),
                    m.EquippedPassive, "the set protocol must arrive with its own first passive");
        }
    }
}
