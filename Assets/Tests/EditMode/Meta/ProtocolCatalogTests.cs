using System;
using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The catalog as a catalog: every sold protocol answers every lookup, the
    /// legality table is a rule rather than a listed pair, and an id this build does not sell
    /// resolves to None instead of reaching a switch with no arm for it.</summary>
    public class ProtocolCatalogTests
    {
        static BoarderProtocolId[] Sold => BoarderProtocol.ShopProtocols();

        [Test]
        public void ThreeProtocolsAreSold_AndTheIdsWereNeverRenumbered()
        {
            Assert.AreEqual(3, Sold.Length);
            Assert.AreEqual(0, (int)BoarderProtocolId.None);
            Assert.AreEqual(1, (int)BoarderProtocolId.Default);
            Assert.AreEqual(2, (int)BoarderProtocolId.Veil);
            Assert.AreEqual(3, (int)BoarderProtocolId.Forge);
            // Doctrine appends after Stock; Stock stays 0 or every placed decoy re-reads.
            Assert.AreEqual(0, (int)DoctrineMode.Stock);
            Assert.AreEqual(1, (int)DoctrineMode.Phantom);
            Assert.AreEqual(2, (int)DoctrineMode.ParallelBays);
            // The six reserved ability ids keep their numbers; the new rows append.
            Assert.AreEqual(5, (int)BoarderAbilityId.Barrage);
            Assert.AreEqual(6, (int)BoarderAbilityId.Rush);
            Assert.AreEqual(7, (int)BoarderAbilityId.Ping);
            Assert.AreEqual(8, (int)BoarderAbilityId.Cloak);
            Assert.AreEqual(9, (int)BoarderAbilityId.FieldDeploy);
            Assert.AreEqual(10, (int)BoarderAbilityId.Overcharge);
            Assert.AreEqual(11, (int)BoarderAbilityId.MagnetHands);
        }

        /// <summary>Criterion 3: distinct crew and distinct personal abilities, pairwise.</summary>
        [Test]
        public void EveryProtocol_HasItsOwnCrewAndPersonalAbility()
        {
            var sold = Sold;
            for (int i = 0; i < sold.Length; i++)
            {
                var crew = BoarderProtocol.CrewAbilityOf(sold[i]);
                var personal = BoarderProtocol.PersonalAbilityOf(sold[i]);
                Assert.AreNotEqual(BoarderAbilityId.None, crew, "crew of " + sold[i]);
                Assert.AreNotEqual(BoarderAbilityId.None, personal, "personal of " + sold[i]);
                Assert.AreNotEqual(crew, personal, "one slot twice on " + sold[i]);

                for (int j = i + 1; j < sold.Length; j++)
                {
                    Assert.AreNotEqual(crew, BoarderProtocol.CrewAbilityOf(sold[j]),
                        sold[i] + " and " + sold[j] + " share a crew ability");
                    Assert.AreNotEqual(personal, BoarderProtocol.PersonalAbilityOf(sold[j]),
                        sold[i] + " and " + sold[j] + " share a personal ability");
                }
            }
        }

        /// <summary>Criterion 4: the two new protocols each own a passive, and PassiveAt
        /// answers with it. Default keeps no passive pick, which is its authored shape.</summary>
        [Test]
        public void NewProtocols_OwnAPassive_AndDefaultStillOwnsNone()
        {
            Assert.AreEqual(0, BoarderProtocol.PassiveCount(BoarderProtocolId.Default));
            Assert.AreEqual(BoarderAbilityId.None,
                BoarderProtocol.PassiveAt(BoarderProtocolId.Default, 0));

            Assert.AreEqual(1, BoarderProtocol.PassiveCount(BoarderProtocolId.Veil));
            Assert.AreEqual(BoarderAbilityId.Ghost,
                BoarderProtocol.PassiveAt(BoarderProtocolId.Veil, 0));

            Assert.AreEqual(1, BoarderProtocol.PassiveCount(BoarderProtocolId.Forge));
            Assert.AreEqual(BoarderAbilityId.MagnetHands,
                BoarderProtocol.PassiveAt(BoarderProtocolId.Forge, 0));

            // Out-of-range indices answer None rather than throwing.
            foreach (var id in Sold)
            {
                Assert.AreEqual(BoarderAbilityId.None, BoarderProtocol.PassiveAt(id, -1));
                Assert.AreEqual(BoarderAbilityId.None,
                    BoarderProtocol.PassiveAt(id, BoarderProtocol.PassiveCount(id)));
            }
        }

        /// <summary>Criterion 5: the full cross product, not a listed pair. Legal exactly when
        /// the protocol is sold and either owns the passive or wears none.</summary>
        [Test]
        public void IsLegalPair_IsTheGeneralRule_AcrossTheFullCrossProduct()
        {
            foreach (BoarderProtocolId p in Enum.GetValues(typeof(BoarderProtocolId)))
            {
                foreach (BoarderAbilityId a in Enum.GetValues(typeof(BoarderAbilityId)))
                {
                    bool expected = BoarderProtocol.IsCatalogued(p)
                        && (a == BoarderAbilityId.None || BoarderProtocol.OwnsPassive(p, a));
                    Assert.AreEqual(expected, BoarderProtocol.IsLegalPair(p, a),
                        "IsLegalPair(" + p + ", " + a + ")");
                }
            }

            // Spot-checks that would survive a rule rewritten back into a listed pair.
            Assert.IsTrue(BoarderProtocol.IsLegalPair(BoarderProtocolId.Veil, BoarderAbilityId.Ghost));
            Assert.IsFalse(BoarderProtocol.IsLegalPair(BoarderProtocolId.Veil, BoarderAbilityId.MagnetHands));
            Assert.IsTrue(BoarderProtocol.IsLegalPair(BoarderProtocolId.Forge, BoarderAbilityId.MagnetHands));
            Assert.IsFalse(BoarderProtocol.IsLegalPair(BoarderProtocolId.Forge, BoarderAbilityId.Ghost));
            Assert.IsFalse(BoarderProtocol.IsLegalPair(BoarderProtocolId.None, BoarderAbilityId.None));
            Assert.IsFalse(BoarderProtocol.IsLegalPair((BoarderProtocolId)99, BoarderAbilityId.None));
        }

        /// <summary>Criterion 7's testable half: no sold protocol answers any sheet row with
        /// an empty string, and no two protocols share a row.</summary>
        [Test]
        public void EverySoldProtocol_FillsEverySheetRow()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var id in Sold)
            {
                var sheet = BoarderProtocol.SheetOf(id);
                Assert.IsNotEmpty(sheet.Name, "name of " + id);
                Assert.IsNotEmpty(sheet.Doctrine, "doctrine of " + id);
                Assert.IsNotEmpty(sheet.Crew, "crew row of " + id);
                Assert.IsNotEmpty(sheet.Personal, "personal row of " + id);
                Assert.IsNotEmpty(sheet.Passive, "passive row of " + id);
                Assert.IsTrue(seen.Add(sheet.Name), "duplicate protocol name " + sheet.Name);
                Assert.IsTrue(seen.Add(sheet.Crew), "two protocols share a crew row");
                Assert.IsTrue(seen.Add(sheet.Personal), "two protocols share a personal row");
            }
            // An id nobody sells answers empty, so a stub row cannot masquerade as a sheet.
            Assert.AreEqual("", BoarderProtocol.SheetOf(BoarderProtocolId.None).Name);
        }

        /// <summary>Criterion 9: all three ids survive a save and load.</summary>
        [Test]
        public void EveryProtocol_RoundTripsThroughSaveAndLoad()
        {
            foreach (var id in Sold)
            {
                var m = MetaState.CreateNew();
                Assert.IsTrue(BoarderProtocol.TryClaim(m, id), "claim " + id);
                var expectedPassive = BoarderProtocol.PassiveCount(id) > 0
                    ? BoarderProtocol.PassiveAt(id, 0)
                    : BoarderAbilityId.None;
                Assert.AreEqual(expectedPassive, m.EquippedPassive, "claimed passive on " + id);

                var loaded = MetaState.FromSnapshotJson(m.ToSnapshotJson());
                Assert.AreEqual(id, loaded.EquippedProtocol, "round-trip of " + id);
                Assert.AreEqual(expectedPassive, loaded.EquippedPassive,
                    "round-trip passive of " + id);
            }
        }

        /// <summary>Criterion 9: an id this build does not sell loads as None. It must not
        /// throw, and it must not survive as an id no switch has an arm for.</summary>
        [Test]
        public void UnknownProtocolId_LoadsAsNone()
        {
            // The equipped pair rides the Boarder, so this is a Boarder document.
            const string json =
                "{\"SaveVersion\":13,\"EquippedProtocol\":42,\"EquippedPassive\":0}";
            var m = BoarderState.FromJson(json);
            Assert.IsNotNull(m, "a current-version Boarder document loads");
            Assert.AreEqual(BoarderProtocolId.None, m.EquippedProtocol);
            Assert.AreEqual(BoarderAbilityId.None, m.EquippedPassive);

            // A sold protocol wearing a passive it does not own keeps the protocol and
            // loses the passive — the protocol itself is still legitimate.
            const string mismatched =
                "{\"SaveVersion\":13,\"EquippedProtocol\":2,\"EquippedPassive\":11}";
            var n = BoarderState.FromJson(mismatched);
            Assert.AreEqual(BoarderProtocolId.Veil, n.EquippedProtocol);
            Assert.AreEqual(BoarderAbilityId.None, n.EquippedPassive);
        }

        [Test]
        public void PassivePick_AcceptsOwned_AndRefusesBorrowed()
        {
            var m = MetaState.CreateNew();
            Assert.IsTrue(BoarderProtocol.TryClaim(m, BoarderProtocolId.Veil));
            Assert.IsTrue(m.SetEquippedPassive(BoarderAbilityId.Ghost));
            Assert.AreEqual(BoarderAbilityId.Ghost, m.EquippedPassive);
            Assert.IsFalse(m.SetEquippedPassive(BoarderAbilityId.MagnetHands));
            Assert.AreEqual(BoarderAbilityId.Ghost, m.EquippedPassive);
        }

        [Test]
        public void DoctrineAndWindows_AreDistinctPerProtocol()
        {
            Assert.AreEqual(DoctrineMode.Stock, BoarderProtocol.DoctrineOf(BoarderProtocolId.Default));
            Assert.AreEqual(DoctrineMode.Phantom, BoarderProtocol.DoctrineOf(BoarderProtocolId.Veil));
            Assert.AreEqual(DoctrineMode.ParallelBays, BoarderProtocol.DoctrineOf(BoarderProtocolId.Forge));

            // A doctrine may widen a bay, never narrow one.
            Assert.AreEqual(1, BoarderProtocol.MaxLiveUnder(DoctrineMode.Stock, 1));
            Assert.AreEqual(1, BoarderProtocol.MaxLiveUnder(DoctrineMode.Phantom, 1));
            Assert.AreEqual(2, BoarderProtocol.MaxLiveUnder(DoctrineMode.ParallelBays, 1));
            Assert.AreEqual(3, BoarderProtocol.MaxLiveUnder(DoctrineMode.ParallelBays, 3));

            // Every sold ability has a window and a cooldown, or its key reads as broken.
            foreach (var id in Sold)
            {
                foreach (var ability in new[]
                {
                    BoarderProtocol.CrewAbilityOf(id),
                    BoarderProtocol.PersonalAbilityOf(id),
                })
                {
                    if (ability == BoarderAbilityId.FieldDeploy)
                        Assert.AreEqual(0f, BoarderProtocol.Window(ability), 1e-5f,
                            "Field Deploy is instant and must not claim a window");
                    else
                        Assert.Greater(BoarderProtocol.Window(ability), 0f, "window of " + ability);
                    Assert.Greater(BoarderProtocol.Cooldown(ability), 0f, "cooldown of " + ability);
                    Assert.IsNotEmpty(BoarderProtocol.DisplayName(ability));
                }
            }
        }

        /// <summary>Only Default's rows multiply fire rate. A Cloak or Redeploy window opens
        /// the same slot Rush does and must buy nothing at the gun.</summary>
        [Test]
        public void FireMultipliersBelongToDefaultAlone()
        {
            Assert.AreEqual(2f, BoarderProtocol.FireMul(BoarderAbilityId.Barrage), 1e-5f);
            Assert.AreEqual(1.5f, BoarderProtocol.FireMul(BoarderAbilityId.Rush), 1e-5f);
            foreach (var ability in new[]
            {
                BoarderAbilityId.Ping, BoarderAbilityId.Cloak,
                BoarderAbilityId.FieldDeploy, BoarderAbilityId.Overcharge,
            })
                Assert.AreEqual(1f, BoarderProtocol.FireMul(ability), 1e-5f, "FireMul of " + ability);
        }

        /// <summary>Renamed 2026-08-28 from <c>TurretArithmetic_RateAndMagazine</c>: the
        /// magazine is no longer the machine's lifecycle on its own. TWO LIMITS NOW, whichever
        /// comes first (Zo: "make turret expire after 60 rounds and 20 seconds"), plus a third
        /// number that is neither of them — OVERCHARGE'S OWN BELT.</summary>
        [Test]
        public void TurretArithmetic_RateMagazineAndClock()
        {
            Assert.AreEqual(0.5f, BoarderProtocol.TurretShotInterval(1f), 1e-5f);
            Assert.AreEqual(0.2f, BoarderProtocol.TurretShotInterval(
                BoarderProtocol.OverchargeTurretRateMul), 1e-5f);
            // THE TIME-BASED BURN IS RETIRED (2026-08-28). It used to bill 18 life a second on
            // top of the rate multiplier, which double-charged one spike. Overcharge is paid
            // for out of its own belt now, and the machine's HP is the enemy's business alone.
            Assert.AreEqual(0f, BoarderProtocol.OverchargeBurnForSeconds(1f), 1e-5f,
                "overcharge is billing a second, separate toll again");

            // THE DEPLOYABLE TEST: a machine must be GONE before Field Deploy comes back, or
            // the player simply always has a turret and standing one stops being a decision.
            // Derived from the live constants so retuning any of them re-answers the question.
            float firingSeconds = BoarderProtocol.TurretShotBudget / BoarderProtocol.TurretShotsPerSecond;
            float life = BoarderProtocol.TurretLifeSeconds;
            float cooldown = BoarderProtocol.Cooldown(BoarderAbilityId.FieldDeploy);
            float binding = life < firingSeconds ? life : firingSeconds;

            // WHICH LIMIT BINDS IS THE THING THAT MOVED. The rounds went 40 -> 60, so the
            // magazine is now EXACTLY 30 s of firing against a 30 s cooldown — a dead tie,
            // and this assertion used to be `Assert.Less(firingSeconds, cooldown)` sitting on
            // it. The clock is what rations the machine; the magazine is only the ceiling on
            // how much it can ever do. Pinned as a strict inequality so a retune that puts the
            // rounds back in charge fails here rather than passing on the tie.
            Assert.AreEqual(life, binding, 1e-4f,
                $"the magazine ({firingSeconds:0.#}s) binds before the clock ({life:0.#}s) - "
                + "TurretLifeSeconds has stopped being what ends a turret");
            Assert.Less(life, firingSeconds,
                "the clock and the magazine expire together, so only one of them is pinned");
            Assert.Less(binding, cooldown,
                $"a turret stands {binding:0.#}s against a {cooldown:0.#}s cooldown - "
                + "it outlives its own rationing, which is what made it read as permanent");
            Assert.Greater(binding, cooldown * 0.4f,
                "the machine is gone so fast it is never worth standing");

            // OVERCHARGE'S BELT IS ITS WINDOW, WRITTEN TWICE. Redlining used to be paid for
            // out of the magazine — "the same currency, at exactly the rate multiplier" — and
            // the old pin for that was `TurretShotBudget / (rate * mul)` compared against
            // `(TurretShotBudget / rate) / mul`, which is the same expression rearranged and
            // was true of ANY constants. The belt is a separate number, so there is finally
            // something to check: the window at the overcharged rate must spend it exactly.
            float overchargeWindow = BoarderProtocol.Window(BoarderAbilityId.Overcharge);
            float overchargedRate = BoarderProtocol.TurretShotsPerSecond
                * BoarderProtocol.OverchargeTurretRateMul;
            Assert.AreEqual(BoarderProtocol.OverchargeShotBudget,
                overchargeWindow * overchargedRate, 1e-4f,
                $"{overchargeWindow:0.#}s at {overchargedRate:0.#}/s is not "
                + BoarderProtocol.OverchargeShotBudget + " rounds - the window and the belt "
                + "have drifted apart, and whichever is smaller is now the real limit");

            // And it must be a SEPARATE belt, not a slice of the magazine: a full redline has
            // to leave the machine standing with ordinary rounds still in it.
            Assert.Less(BoarderProtocol.OverchargeShotBudget, BoarderProtocol.TurretShotBudget,
                "a full overcharge empties the magazine as well - the spike costs the machine");
            // A redline must also be shorter than the machine's life, or the belt can never
            // run out before the clock does and its size stops meaning anything.
            Assert.Less(overchargeWindow, life,
                "the overcharge window outlasts the turret - the belt is never reached");
        }

        /// <summary>Zone sources set rather than stack; self sources multiply on top.</summary>
        [Test]
        public void ComposeFireMul_SelfTimesStrongestZone()
        {
            Assert.AreEqual(1f, BoarderProtocol.ComposeFireMul(1f, 1f), 1e-5f);
            Assert.AreEqual(1.35f, BoarderProtocol.ComposeFireMul(1f, BoarderProtocol.TurretAuraFireMul), 1e-5f);
            Assert.AreEqual(1.4f * 1.35f, BoarderProtocol.ComposeFireMul(
                BoarderProtocol.OverchargeSelfFireMul, BoarderProtocol.TurretAuraFireMul), 1e-4f);
            // Zone is capped, so a turret aura inside a Barrage cannot exceed the Barrage cap.
            Assert.AreEqual(3f, BoarderProtocol.ComposeFireMul(1.5f, 9f), 1e-5f);
            Assert.AreEqual(1f, BoarderProtocol.ComposeFireMul(0.2f, 0.2f), 1e-5f);
        }

        [Test]
        public void Turret_PlacesFiresItselfDry_AndCanStillBeKilledEarly()
        {
            // THE MAGAZINE IS THE CEILING (Zo, 2026-08-28: "expire after 60 rounds and 20
            // seconds"). This fixture used to prove the machine died to an overcharge burn on
            // a clock; that toll is retired. Rounds are one of the TWO limits that end a
            // turret and this is the one exercised here — the life clock never advances
            // because nothing calls TickLife, so the rounds are what run out.
            // Turret_ExpiresOnTheClock_AndRedliningSpendsItsOwnBelt drives the other limit and
            // the overcharge belt. Enemy damage is still a way to lose one EARLY, which is the
            // second half of this test — running dry and being killed must stay apart.
            var t = new TurretState();
            t.ResetForRun();
            Assert.IsFalse(t.IsLive);
            Assert.IsTrue(t.TryPlace(1f, 0f, 2f, out _));
            Assert.IsTrue(t.IsLive);
            Assert.AreEqual(BoarderProtocol.TurretMaxHp, t.Hp, 1e-4f);
            Assert.AreEqual(BoarderProtocol.TurretShotBudget, t.ShotsRemaining,
                "a fresh machine did not come loaded");

            // Every round but the last leaves it standing; the last one ends it.
            for (int i = 0; i < BoarderProtocol.TurretShotBudget - 1; i++)
            {
                Assert.IsFalse(t.SpendShot(), "round " + (i + 1) + " ended the turret early");
                Assert.IsTrue(t.IsLive);
            }
            Assert.IsTrue(t.SpendShot(), "the last round did not end the machine");
            Assert.IsFalse(t.IsLive);
            Assert.IsFalse(t.Overcharged, "a spent machine is not overcharged");
            Assert.AreEqual(BoarderProtocol.TurretShotBudget, t.ShotsFired);
            Assert.AreEqual(0f, t.DamageTaken, 1e-5f,
                "running dry was recorded as enemy damage - the two events must read apart");

            // A spent machine cannot fire a farewell round.
            Assert.IsFalse(t.SpendShot(), "a dead turret fired");

            // AND IT CAN STILL BE KILLED EARLY. Losing the machine to the Bastion with rounds
            // left is the whole reason it is mortal at all; if this ever stops holding, the
            // turret is back to being the player's to keep.
            var u = new TurretState();
            u.ResetForRun();
            u.TryPlace(0f, 0f, 0f, out _);
            Assert.IsFalse(u.SpendShot());
            Assert.IsTrue(u.TakeDamage(BoarderProtocol.TurretMaxHp));
            Assert.IsFalse(u.IsLive, "enemy damage could not take a loaded turret");
            Assert.Greater(u.ShotsRemaining, 0, "it died with rounds left, which is the point");
            Assert.AreEqual(BoarderProtocol.TurretMaxHp, u.DamageTaken, 1e-4f);
        }

        /// <summary>THE OTHER LIMIT, AND THE SEPARATE BELT (Zo, 2026-08-28: "make turret expire
        /// after 60 rounds and 20 seconds. Overcharge should get its own ammunition count and
        /// 8 seconds. So turret can remain and not be punished with its normal ammunition
        /// count"). Rounds alone let a machine covering a quiet lane stand forever, and
        /// redlining out of the magazine charged the player the turret's whole ordinary life
        /// for one spike.</summary>
        [Test]
        public void Turret_ExpiresOnTheClock_AndRedliningSpendsItsOwnBelt()
        {
            // THE CLOCK ENDS A QUIET MACHINE. It never fires a round here, so the magazine on
            // its own would leave it standing until the enemy chose to shoot it.
            var quiet = new TurretState();
            quiet.ResetForRun();
            Assert.IsTrue(quiet.TryPlace(0f, 0f, 0f, out _));
            Assert.AreEqual(BoarderProtocol.TurretLifeSeconds, quiet.LifeRemaining, 1e-4f,
                "a fresh machine did not come with its clock wound");

            Assert.IsFalse(quiet.TickLife(BoarderProtocol.TurretLifeSeconds - 1f),
                "the machine expired with a second still on the clock");
            Assert.IsTrue(quiet.IsLive);
            Assert.AreEqual(1f, quiet.LifeRemaining, 1e-4f);
            Assert.IsTrue(quiet.TickLife(1f), "the last second did not expire the machine");
            Assert.IsFalse(quiet.IsLive);
            Assert.AreEqual(0, quiet.ShotsFired,
                "it expired on the clock, so it must have expired with a full magazine");
            Assert.AreEqual(0f, quiet.DamageTaken, 1e-5f,
                "expiring was recorded as enemy damage - the two events must read apart");
            Assert.IsFalse(quiet.TickLife(1f), "an expired machine expired a second time");

            // THE BELT PAYS FOR THE REDLINE AND FOR NOTHING ELSE. Running it dry drops the
            // machine back to ordinary fire with its magazine untouched — that is the whole
            // reason the belt exists rather than a 2.5x drain on the rounds it already had.
            var hot = new TurretState();
            hot.ResetForRun();
            hot.TryPlace(0f, 0f, 0f, out _);
            hot.SetOvercharged(true);
            Assert.IsTrue(hot.Overcharged);
            Assert.AreEqual(BoarderProtocol.OverchargeShotBudget, hot.OverchargeShotsRemaining,
                "the redline came up with an empty belt");
            for (int i = 0; i < BoarderProtocol.OverchargeShotBudget; i++)
                Assert.IsFalse(hot.SpendShot(),
                    "overcharge round " + (i + 1) + " ended the machine");

            Assert.IsTrue(hot.IsLive, "running the belt dry killed the turret");
            Assert.IsFalse(hot.Overcharged, "the belt is empty and the machine is still redlined");
            Assert.AreEqual(0, hot.OverchargeShotsRemaining);
            Assert.AreEqual(BoarderProtocol.TurretShotBudget, hot.ShotsRemaining,
                "the redline was billed to the magazine - that is the double charge the belt "
                + "was added to retire");
            Assert.AreEqual(BoarderProtocol.OverchargeShotBudget, hot.ShotsFired);
            Assert.AreEqual(BoarderProtocol.TurretShotInterval(1f), hot.ShotInterval(), 1e-5f,
                "an empty belt is still firing at the overcharged rate");

            // From here it is an ordinary turret, with everything it started with.
            Assert.IsFalse(hot.SpendShot());
            Assert.AreEqual(BoarderProtocol.TurretShotBudget - 1, hot.ShotsRemaining);
            Assert.IsTrue(hot.IsLive);
        }

        /// <summary>Field Deploy is one machine: standing a new one replaces the old, and
        /// the fresh machine carries none of the previous one's wear.</summary>
        [Test]
        public void Turret_RedeployReplacesAndResetsWear()
        {
            var t = new TurretState();
            t.ResetForRun();
            t.TryPlace(1f, 0f, 1f, out _);
            t.TakeDamage(40f);
            // Wear is three things now, not one: damage, rounds, and the clock. Spend some of
            // each so a redeploy that resets only the old one is caught here.
            t.SetOvercharged(true);
            t.SpendShot();
            t.SetOvercharged(false);
            t.SpendShot();
            t.TickLife(BoarderProtocol.TurretLifeSeconds * 0.5f);
            Assert.AreEqual(40f, t.DamageTaken, 1e-4f);
            Assert.Less(t.LifeRemaining, BoarderProtocol.TurretLifeSeconds);
            Assert.Less(t.ShotsRemaining, BoarderProtocol.TurretShotBudget);
            Assert.Less(t.OverchargeShotsRemaining, BoarderProtocol.OverchargeShotBudget);

            Assert.IsTrue(t.TryPlace(5f, 0f, 5f, out _));
            Assert.AreEqual(BoarderProtocol.TurretMaxHp, t.Hp, 1e-4f);
            Assert.AreEqual(0f, t.DamageTaken, 1e-5f);
            Assert.AreEqual(0f, t.LifeBurned, 1e-5f);
            Assert.AreEqual(BoarderProtocol.TurretShotBudget, t.ShotsRemaining,
                "the new machine inherited the old one's spent magazine");
            Assert.AreEqual(BoarderProtocol.TurretLifeSeconds, t.LifeRemaining, 1e-4f,
                "the new machine inherited the old one's clock");
            Assert.AreEqual(BoarderProtocol.OverchargeShotBudget, t.OverchargeShotsRemaining,
                "the new machine inherited the old one's overcharge belt");
            Assert.AreEqual(0, t.ShotsFired);
            Assert.IsFalse(t.Overcharged);
            Assert.AreEqual(5f, t.PosX, 1e-5f);
        }
    }
}
