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

                var loaded = MetaState.FromJson(m.ToJson());
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
            const string json =
                "{\"SaveVersion\":11,\"BankedSalvage\":0,\"LifetimeSalvageEarned\":0," +
                "\"LifetimeSalvageSpent\":0,\"EquippedProtocol\":42,\"EquippedPassive\":0}";
            var m = MetaState.FromJson(json);
            Assert.AreEqual(BoarderProtocolId.None, m.EquippedProtocol);
            Assert.AreEqual(BoarderAbilityId.None, m.EquippedPassive);

            // A sold protocol wearing a passive it does not own keeps the protocol and
            // loses the passive — the protocol itself is still legitimate.
            const string mismatched =
                "{\"SaveVersion\":11,\"BankedSalvage\":0,\"LifetimeSalvageEarned\":0," +
                "\"LifetimeSalvageSpent\":0,\"EquippedProtocol\":2,\"EquippedPassive\":11}";
            var n = MetaState.FromJson(mismatched);
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

        [Test]
        public void TurretArithmetic_RateAndBurn()
        {
            Assert.AreEqual(0.5f, BoarderProtocol.TurretShotInterval(1f), 1e-5f);
            Assert.AreEqual(0.2f, BoarderProtocol.TurretShotInterval(
                BoarderProtocol.OverchargeTurretRateMul), 1e-5f);
            Assert.AreEqual(18f, BoarderProtocol.OverchargeBurnForSeconds(1f), 1e-5f);
            Assert.AreEqual(0f, BoarderProtocol.OverchargeBurnForSeconds(0f), 1e-5f);

            // A full window on a healthy turret must wound it without killing it, or the
            // ability is either free or suicide. Both ends are the design.
            float full = BoarderProtocol.OverchargeBurnForSeconds(
                BoarderProtocol.Window(BoarderAbilityId.Overcharge));
            Assert.Less(full, BoarderProtocol.TurretMaxHp, "a full overcharge must not kill a fresh turret");
            Assert.Greater(full, BoarderProtocol.TurretMaxHp * 0.5f, "a full overcharge must cost real life");
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
        public void Turret_PlacesWoundsBurnsAndDies()
        {
            var t = new TurretState();
            t.ResetForRun();
            Assert.IsFalse(t.IsLive);
            Assert.IsTrue(t.TryPlace(1f, 0f, 2f, out _));
            Assert.IsTrue(t.IsLive);
            Assert.AreEqual(BoarderProtocol.TurretMaxHp, t.Hp, 1e-4f);

            // Overcharge does nothing until it is switched on.
            float burned;
            Assert.IsFalse(t.BurnOvercharge(1f, out burned));
            Assert.AreEqual(0f, burned, 1e-5f);

            t.SetOvercharged(true);
            Assert.IsTrue(t.Overcharged);
            Assert.AreEqual(0.2f, t.ShotInterval(), 1e-5f);
            Assert.IsFalse(t.BurnOvercharge(1f, out burned));
            Assert.AreEqual(BoarderProtocol.OverchargeHpPerSecond, burned, 1e-4f);
            Assert.AreEqual(burned, t.LifeBurned, 1e-4f);
            Assert.AreEqual(0f, t.DamageTaken, 1e-5f, "burn is not enemy damage");

            // Enemy damage finishes what the burn started, and the two tallies stay apart.
            float left = t.Hp;
            Assert.AreEqual(BoarderProtocol.TurretMaxHp - burned, left, 1e-4f);
            Assert.IsTrue(t.TakeDamage(left));
            Assert.IsFalse(t.IsLive, "enemy damage took the last of it");
            Assert.AreEqual(left, t.DamageTaken, 1e-4f);
            Assert.AreEqual(burned, t.LifeBurned, 1e-4f);

            var u = new TurretState();
            u.ResetForRun();
            u.TryPlace(0f, 0f, 0f, out _);
            u.TakeDamage(BoarderProtocol.TurretMaxHp - 10f);
            u.SetOvercharged(true);
            Assert.IsTrue(u.BurnOvercharge(1f, out burned), "the burn should finish a wounded turret");
            Assert.IsFalse(u.IsLive);
            Assert.IsFalse(u.Overcharged, "a dead machine is not overcharged");
            Assert.AreEqual(10f, burned, 1e-4f, "the burn cannot take more life than remains");
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
            Assert.AreEqual(40f, t.DamageTaken, 1e-4f);

            Assert.IsTrue(t.TryPlace(5f, 0f, 5f, out _));
            Assert.AreEqual(BoarderProtocol.TurretMaxHp, t.Hp, 1e-4f);
            Assert.AreEqual(0f, t.DamageTaken, 1e-5f);
            Assert.AreEqual(0f, t.LifeBurned, 1e-5f);
            Assert.IsFalse(t.Overcharged);
            Assert.AreEqual(5f, t.PosX, 1e-5f);
        }
    }
}
