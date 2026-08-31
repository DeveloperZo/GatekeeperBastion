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
            Assert.AreEqual(1, (int)BoarderProtocolId.Depot);
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
            Assert.AreEqual(12, (int)BoarderAbilityId.Emp);
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

        /// <summary>VEIL owns Ghost. FORGE's passive is unfilled on purpose (sheet TODO, no
        /// pick). Default keeps no passive pick, which is its authored shape.</summary>
        [Test]
        public void NewProtocols_OwnAPassive_AndDefaultStillOwnsNone()
        {
            Assert.AreEqual(0, BoarderProtocol.PassiveCount(BoarderProtocolId.Depot));
            Assert.AreEqual(BoarderAbilityId.None,
                BoarderProtocol.PassiveAt(BoarderProtocolId.Depot, 0));

            Assert.AreEqual(1, BoarderProtocol.PassiveCount(BoarderProtocolId.Veil));
            Assert.AreEqual(BoarderAbilityId.Ghost,
                BoarderProtocol.PassiveAt(BoarderProtocolId.Veil, 0));

            Assert.AreEqual(0, BoarderProtocol.PassiveCount(BoarderProtocolId.Forge));
            Assert.AreEqual(BoarderAbilityId.None,
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
            Assert.IsFalse(BoarderProtocol.IsLegalPair(BoarderProtocolId.Veil, BoarderAbilityId.Overcharge));
            Assert.IsTrue(BoarderProtocol.IsLegalPair(BoarderProtocolId.Forge, BoarderAbilityId.None));
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

        [Test]
        public void ForgeSheet_NamesTheOutriderAndProximityGatedPairing()
        {
            Assert.AreEqual(BoarderAbilityId.Emp,
                BoarderProtocol.PersonalAbilityOf(BoarderProtocolId.Forge));
            StringAssert.Contains("proximity-gated", BoarderSheets.CrewForge);
            StringAssert.Contains("Outrider", BoarderSheets.PersonalForge);
            StringAssert.Contains("EMP", BoarderSheets.PersonalForge);
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
            Assert.IsFalse(m.SetEquippedPassive(BoarderAbilityId.Overcharge));
            Assert.AreEqual(BoarderAbilityId.Ghost, m.EquippedPassive);
        }

        [Test]
        public void DoctrineAndWindows_AreDistinctPerProtocol()
        {
            Assert.AreEqual(DoctrineMode.Stock, BoarderProtocol.DoctrineOf(BoarderProtocolId.Depot));
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
                    if (ability == BoarderAbilityId.FieldDeploy || ability == BoarderAbilityId.Emp)
                        Assert.AreEqual(0f, BoarderProtocol.Window(ability), 1e-5f,
                            ability + " is instant and must not claim a window");
                    else
                        Assert.Greater(BoarderProtocol.Window(ability), 0f, "window of " + ability);
                    Assert.Greater(BoarderProtocol.Cooldown(ability), 0f, "cooldown of " + ability);
                    Assert.IsNotEmpty(BoarderProtocol.DisplayName(ability));
                }
            }
        }

        /// <summary>Only Dump (Barrage id) multiplies fire rate on Depot. Feed regenerates
        /// clip instead. Cloak / Field Deploy / etc. buy nothing at the gun.</summary>
        [Test]
        public void FireMultipliersBelongToDefaultAlone()
        {
            Assert.AreEqual(2f, BoarderProtocol.FireMul(BoarderAbilityId.Barrage), 1e-5f);
            Assert.AreEqual(1f, BoarderProtocol.FireMul(BoarderAbilityId.Rush), 1e-5f);
            foreach (var ability in new[]
            {
                BoarderAbilityId.Ping, BoarderAbilityId.Cloak,
                BoarderAbilityId.FieldDeploy, BoarderAbilityId.Overcharge,
                BoarderAbilityId.Emp,
            })
                Assert.AreEqual(1f, BoarderProtocol.FireMul(ability), 1e-5f, "FireMul of " + ability);
        }

        /// <summary>Rate arithmetic and the low-end charge dials. Lifecycle is
        /// <see cref="TurretStateTests"/> — charge, Special, Exhausted — not a clock or a belt.</summary>
        [Test]
        public void TurretArithmetic_RateAndChargeDials()
        {
            Assert.AreEqual(0.5f, BoarderProtocol.TurretShotInterval(1f), 1e-5f);
            Assert.AreEqual(0.2f, BoarderProtocol.TurretShotInterval(
                BoarderProtocol.OverchargeTurretRateMul), 1e-5f);

            Assert.Greater(BoarderProtocol.TurretChargePerShot, 0f);
            Assert.Greater(BoarderProtocol.TurretChargeMax, 0f);
            Assert.Greater(BoarderProtocol.TurretExhaustedSeconds, 0f);
            Assert.Greater(BoarderProtocol.TurretHoldThresholdSeconds, 0f);
            Assert.Less(BoarderProtocol.TurretHoldThresholdSeconds, 1f);

            // A full meter at the overcharged rate must be a short spike, not a second life.
            float fullSeconds = BoarderProtocol.TurretChargeMax
                / (BoarderProtocol.TurretShotsPerSecond * BoarderProtocol.OverchargeTurretRateMul
                    * BoarderProtocol.TurretChargePerShot);
            Assert.Less(fullSeconds, BoarderProtocol.Cooldown(BoarderAbilityId.FieldDeploy),
                "a full overcharge outlasts Field Deploy's cooldown — the spike is a fixture");
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
    }
}
