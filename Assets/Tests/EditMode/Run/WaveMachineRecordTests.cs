using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Criterion 10: one Boarding's log names, for each Wave, which machine was
    /// deployed first and what each machine's charge was when its special ran. The rule with
    /// teeth is the last one here — a machine that stood through the previous Wave is this
    /// Wave's first, and clearing the latch at the boundary once put that backwards in a real
    /// log. Mirrors <see cref="OutriderStateTests"/>.</summary>
    public class WaveMachineRecordTests
    {
        [Test]
        public void FirstDeploy_NamesTheMachineInTheLine()
        {
            var r = new WaveMachineRecord();
            r.BeginWave(false, false);
            r.NoteDeploy("outrider");

            Assert.AreEqual("outrider", r.FirstMachine);
            Assert.AreEqual(
                "machines first=outrider turret-special-charge=- outrider-special-charge=-",
                r.Line());
        }

        [Test]
        public void SecondDeploy_DoesNotOverwriteTheFirst()
        {
            var r = new WaveMachineRecord();
            r.BeginWave(false, false);
            r.NoteDeploy("turret");
            r.NoteDeploy("outrider");

            Assert.AreEqual("turret", r.FirstMachine, "a machine joining later stole the lead");
            StringAssert.Contains("first=turret", r.Line());
        }

        [Test]
        public void EmptyDeployName_IsIgnoredRatherThanLatched()
        {
            var r = new WaveMachineRecord();
            r.BeginWave(false, false);
            r.NoteDeploy(null);
            r.NoteDeploy("");
            Assert.IsNull(r.Line(), "a nameless deploy opened the record");

            r.NoteDeploy("turret");
            Assert.AreEqual("turret", r.FirstMachine);
        }

        [Test]
        public void MachineSurvivingIntoTheNextWave_IsThatWavesFirst()
        {
            // The 2026-08-29_22-53-02 case: the outrider led wave 1, the turret joined, and
            // both were still standing when wave 2 opened. Clearing the latch at the boundary
            // logged wave 2 as turret-first because the turret is named first when both stand.
            var r = new WaveMachineRecord();
            r.BeginWave(false, false);
            r.NoteDeploy("outrider");
            r.NoteDeploy("turret");

            r.BeginWave(hasTurret: true, hasOutrider: true);
            Assert.AreEqual("outrider", r.FirstMachine,
                "the Wave boundary re-decided who led with both machines already standing");
        }

        [Test]
        public void LoneSurvivor_LeadsTheNextWaveWithoutADeployCall()
        {
            var r = new WaveMachineRecord();
            r.BeginWave(hasTurret: false, hasOutrider: true);
            Assert.AreEqual("outrider", r.FirstMachine);

            var t = new WaveMachineRecord();
            t.BeginWave(hasTurret: true, hasOutrider: false);
            Assert.AreEqual("turret", t.FirstMachine);
        }

        [Test]
        public void BothStandingFromNothing_NamesTheTurret()
        {
            var r = new WaveMachineRecord();
            r.BeginWave(hasTurret: true, hasOutrider: true);
            Assert.AreEqual("turret", r.FirstMachine);
        }

        [Test]
        public void SpecialCharge_IsCapturedPerMachineAtCast()
        {
            var r = new WaveMachineRecord();
            r.BeginWave(false, false);
            r.NoteDeploy("turret");
            r.NoteTurretSpecial(7f);

            Assert.AreEqual(
                "machines first=turret turret-special-charge=7/"
                    + ((int)BoarderProtocol.TurretChargeMax).ToString()
                    + " outrider-special-charge=-",
                r.Line(),
                "one machine's special leaked into the other's slot");

            r.NoteOutriderSpecial(3f);
            Assert.AreEqual(
                "machines first=turret turret-special-charge=7/"
                    + ((int)BoarderProtocol.TurretChargeMax).ToString()
                    + " outrider-special-charge=3/"
                    + ((int)BoarderProtocol.OutriderChargeMax).ToString(),
                r.Line());
        }

        [Test]
        public void SpecialCharge_TruncatesTowardZeroTheWayTheReadoutDoes()
        {
            var r = new WaveMachineRecord();
            r.BeginWave(false, false);
            r.NoteDeploy("outrider");
            r.NoteOutriderSpecial(4.9f);
            StringAssert.Contains("outrider-special-charge=4/", r.Line());
        }

        [Test]
        public void NewWave_ClearsBothSpecialLatchesButNotTheLead()
        {
            var r = new WaveMachineRecord();
            r.BeginWave(false, false);
            r.NoteDeploy("turret");
            r.NoteTurretSpecial(9f);
            r.NoteOutriderSpecial(2f);

            r.BeginWave(hasTurret: true, hasOutrider: true);
            Assert.AreEqual(
                "machines first=turret turret-special-charge=- outrider-special-charge=-",
                r.Line(),
                "last Wave's specials were reported against this Wave");
        }

        [Test]
        public void NeitherMachineDeployed_ProducesNoLine()
        {
            var r = new WaveMachineRecord();
            Assert.IsNull(r.Line(), "a record nobody touched still wrote a line");

            r.BeginWave(hasTurret: false, hasOutrider: false);
            Assert.IsNull(r.Line());

            // A special without a deploy is impossible in the game, but the line must still
            // stay silent rather than name an empty machine.
            r.NoteTurretSpecial(5f);
            Assert.IsNull(r.Line());
        }

        [Test]
        public void EveryWaveIsFreshWhenBothMachinesFell()
        {
            var r = new WaveMachineRecord();
            r.BeginWave(false, false);
            r.NoteDeploy("outrider");
            r.BeginWave(hasTurret: false, hasOutrider: false);

            Assert.IsNull(r.Line(), "a Wave with nothing standing inherited the last one's lead");
            r.NoteDeploy("turret");
            Assert.AreEqual("turret", r.FirstMachine);
        }
    }

    /// <summary>The charge readout is reached every frame from the HUD's LateUpdate, so the
    /// strings are a fixed table rather than a concatenation. These tests assert identity, not
    /// equality — an equal string built per call is exactly the defect the table removed.
    /// </summary>
    public class MachineChargeLabelTests
    {
        [Test]
        public void ChargeLabel_ReadsAsChargeOverMaxForBothMachines()
        {
            Assert.AreEqual("0/" + ((int)BoarderProtocol.TurretChargeMax).ToString(),
                MachineChargeLabels.OfTurret(0f));
            Assert.AreEqual(
                ((int)BoarderProtocol.TurretChargeMax).ToString()
                    + "/" + ((int)BoarderProtocol.TurretChargeMax).ToString(),
                MachineChargeLabels.OfTurret(BoarderProtocol.TurretChargeMax));
            Assert.AreEqual("0/" + ((int)BoarderProtocol.OutriderChargeMax).ToString(),
                MachineChargeLabels.OfOutrider(0f));
            Assert.AreEqual(
                ((int)BoarderProtocol.OutriderChargeMax).ToString()
                    + "/" + ((int)BoarderProtocol.OutriderChargeMax).ToString(),
                MachineChargeLabels.OfOutrider(BoarderProtocol.OutriderChargeMax));
        }

        // Reference identity is the whole assertion, so it is spelled out rather than left to
        // an equality assert that a freshly concatenated string would also pass.
        static void SameInstance(string a, string b, string message)
        {
            Assert.IsTrue(ReferenceEquals(a, b), message);
        }

        [Test]
        public void RepeatedAsks_ReturnTheSameStringInstance()
        {
            SameInstance(MachineChargeLabels.OfTurret(4f), MachineChargeLabels.OfTurret(4f),
                "the per-frame path built a new string");
            SameInstance(MachineChargeLabels.OfTurret(4.75f), MachineChargeLabels.OfTurret(4f),
                "a fractional charge missed the table it should have truncated into");
            SameInstance(MachineChargeLabels.OfOutrider(1f), MachineChargeLabels.OfOutrider(1f),
                "the outrider meter is not cached");
        }

        [Test]
        public void OutOfRangeCharge_ClampsIntoTheTableRatherThanAllocating()
        {
            SameInstance(MachineChargeLabels.OfTurret(0f), MachineChargeLabels.OfTurret(-3f),
                "a negative charge fell out of the table");
            SameInstance(
                MachineChargeLabels.OfTurret(BoarderProtocol.TurretChargeMax),
                MachineChargeLabels.OfTurret(BoarderProtocol.TurretChargeMax + 5f),
                "a charge past the meter fell out of the table");
        }
    }
}
