using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>M30: decoy payload supply, pull refusals, and PayloadId save stability.</summary>
    public class DecoyTests
    {
        [Test]
        public void ChargesAreSpentOnPlaceAndRefusedAtZero()
        {
            var s = NewDecoyState();
            Assert.AreEqual(3, s.ChargesRemaining);
            Assert.IsTrue(s.TryPlace(1f, 0f, 1f, out _));
            s.ClearLive();
            Assert.IsTrue(s.TryPlace(2f, 0f, 2f, out _));
            s.ClearLive();
            Assert.IsTrue(s.TryPlace(3f, 0f, 3f, out _));
            s.ClearLive();
            Assert.AreEqual(0, s.ChargesRemaining);
            Assert.IsFalse(s.TryPlace(4f, 0f, 4f, out string reason));
            Assert.AreEqual("no charges remaining", reason);
        }

        [Test]
        public void OnlyOneLiveAtATime()
        {
            var s = NewDecoyState();
            Assert.IsTrue(s.TryPlace(0f, 0f, 0f, out _));
            int charges = s.ChargesRemaining;
            Assert.IsFalse(s.TryPlace(1f, 0f, 1f, out string reason));
            Assert.AreEqual("one decoy already live", reason);
            Assert.AreEqual(charges, s.ChargesRemaining, "refused place spends nothing");
        }

        [Test]
        public void AbsorbedSecondsIsDamageOverThreeFourThree()
        {
            var s = NewDecoyState();
            Assert.IsTrue(s.TryPlace(0f, 0f, 0f, out _));
            // 120 HP of intake → 120 / 3.43 ≈ 34.985 → reports 35.0 at one decimal for the milestone.
            float remaining = 120f;
            while (remaining > 0f)
            {
                float hit = remaining > 4f ? 4f : remaining;
                s.TakeDamage(hit);
                remaining -= hit;
            }
            Assert.AreEqual(120f / 3.43f, s.AbsorbedSeconds, 1e-4f);
            Assert.AreEqual(35.0f, s.AbsorbedSeconds, 0.02f);
        }

        [Test]
        public void DecoyDiesExactlyAtZeroHp()
        {
            var s = NewDecoyState();
            Assert.IsTrue(s.TryPlace(0f, 0f, 0f, out _));
            Assert.IsFalse(s.TakeDamage(119f));
            Assert.IsTrue(s.IsLive);
            Assert.AreEqual(1f, s.Hp, 1e-5f);
            Assert.IsTrue(s.TakeDamage(1f));
            Assert.IsFalse(s.IsLive);
            Assert.AreEqual(0f, s.Hp, 1e-5f);
            Assert.IsFalse(s.TakeDamage(10f), "dead decoy takes no more");
        }

        [Test]
        public void CommittedDefaultIsNotPulled()
        {
            Assert.IsFalse(DecoyState.ShouldPull(
                GatekeeperKind.Default, committedToHatch: true,
                decoyLive: true, distanceToDecoy: 5f, pullRadius: 12f));
            Assert.IsTrue(DecoyState.ShouldPull(
                GatekeeperKind.Default, committedToHatch: false,
                decoyLive: true, distanceToDecoy: 5f, pullRadius: 12f));
        }

        [Test]
        public void ThiefIsNeverPulled()
        {
            Assert.IsFalse(DecoyState.ShouldPull(
                GatekeeperKind.Thief, committedToHatch: false,
                decoyLive: true, distanceToDecoy: 0.5f, pullRadius: 12f));
            Assert.IsFalse(DecoyState.ShouldPull(
                GatekeeperKind.Thief, committedToHatch: true,
                decoyLive: true, distanceToDecoy: 0.5f, pullRadius: 12f));
        }

        [Test]
        public void PayloadIdIsStableAcrossSave()
        {
            Assert.AreEqual(0, (int)PayloadId.None);
            Assert.AreEqual(1, (int)PayloadId.Decoy);

            var m = MetaState.CreateNew();
            m.BankedSalvage = 1000L;
            m.SetPayloadOwned(PayloadId.Decoy, true);
            Assert.IsTrue(m.SetEquippedPayload(PayloadId.Decoy));
            string json = m.ToSnapshotJson();

            var loaded = MetaState.FromSnapshotJson(json);
            Assert.AreEqual(SaveFormat.CurrentVersion, loaded.SaveVersion);
            Assert.IsTrue(loaded.OwnsPayload(PayloadId.Decoy));
            Assert.AreEqual(PayloadId.Decoy, loaded.EquippedDrone);
        }

        [Test]
        public void OnePayloadPerRun()
        {
            var m = MetaState.CreateNew();
            m.SetPayloadOwned(PayloadId.Decoy, true);
            Assert.IsTrue(m.SetEquippedPayload(PayloadId.Decoy));
            m.PayloadLoadoutLocked = true;
            Assert.IsFalse(m.SetEquippedPayload(PayloadId.None));
            Assert.AreEqual(PayloadId.Decoy, m.EquippedDrone);
        }

        static DecoyState NewDecoyState()
        {
            var s = new DecoyState();
            s.ResetForRun(PayloadId.Decoy);
            return s;
        }
    }
}
