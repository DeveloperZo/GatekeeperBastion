using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Zone 1's shape (Docs/Demo - Single/1-first-terminal.md): two terminals, one Wave each,
    /// the second waits on the first, the Hatch waits on both, and none of it applies once
    /// the demo is off — plus the zone table those rules now forward to, including zone 2's
    /// three-Wave span and the edges an unauthored zone or id has to answer for
    /// (Docs/Demo - Single/5-repeat-to-control-room.md).</summary>
    public class FirstTerminalTests
    {
        [Test]
        public void WavesFor_ZoneOneIsOneWavePerTrigger_ZoneTwoIsThree()
        {
            Assert.AreEqual(1, FirstTerminal.WavesFor("T1"));
            Assert.AreEqual(1, FirstTerminal.WavesFor("T2"));
            Assert.AreEqual(3, FirstTerminal.WavesFor("T3"),
                "zone 2 is one terminal running three consecutive Waves per trigger");
            Assert.AreEqual(1, FirstTerminal.WavesFor("T4"),
                "an id the table does not author still answers the terminal default");
        }

        [Test]
        public void ZoneOf_IsZeroForAnUnauthoredId()
        {
            Assert.AreEqual(1, DemoZones.ZoneOf("T1"));
            Assert.AreEqual(1, DemoZones.ZoneOf("T2"));
            Assert.AreEqual(2, DemoZones.ZoneOf("T3"));
            Assert.AreEqual(0, DemoZones.ZoneOf("T4"));
            Assert.AreEqual(0, DemoZones.ZoneOf("Master"),
                "Master Control is its own beat, not a zone with terminals and a door");
            Assert.AreEqual(0, DemoZones.ZoneOf(null));
            Assert.AreEqual(0, DemoZones.ZoneOf(string.Empty));
        }

        [Test]
        public void ZoneOfWave_FloorsPastTheLastAuthoredWave()
        {
            Assert.AreEqual(1, DemoZones.ZoneOfWave(1));
            Assert.AreEqual(1, DemoZones.ZoneOfWave(2));
            Assert.AreEqual(2, DemoZones.ZoneOfWave(3));
            Assert.AreEqual(2, DemoZones.ZoneOfWave(5));
            Assert.AreEqual(DemoZones.ZoneCount, DemoZones.ZoneOfWave(6),
                "a Wave past the last zone floors to it — the Wave is at least that deep");
            Assert.AreEqual(DemoZones.ZoneCount, DemoZones.ZoneOfWave(99));
        }

        [Test]
        public void ZoneTwo_SpansWavesThreeToFive_BehindOneTerminal()
        {
            Assert.AreEqual(3, DemoZones.FirstWave(2));
            Assert.AreEqual(5, DemoZones.EndWave(2));
            Assert.AreEqual(3, DemoZones.WavesPerTrigger(2),
                "one trigger runs the whole span — no Prep between them");
            Assert.AreEqual(1, DemoZones.TerminalCount(2));
            Assert.AreEqual("T3", DemoZones.DeepestTerminalId(2),
                "the zone's only terminal is also its deepest — it carries the retrigger");
            Assert.AreEqual("D2", DemoZones.DoorId(2));
        }

        [Test]
        public void UnauthoredZone_AnswersEmptyRatherThanThrowing()
        {
            Assert.AreEqual(0, DemoZones.TerminalCount(0));
            Assert.AreEqual(0, DemoZones.TerminalCount(DemoZones.ZoneCount + 1));
            Assert.IsNull(DemoZones.TerminalIdAt(1, 9));
            Assert.IsNull(DemoZones.DeepestTerminalId(DemoZones.ZoneCount + 1));
            Assert.IsNull(DemoZones.DoorId(0));
            Assert.AreEqual(0, DemoZones.FirstWave(0));
            Assert.AreEqual(0, DemoZones.EndWave(DemoZones.ZoneCount + 1));
            Assert.AreEqual(0, DemoZones.WavesPerTrigger(0));
            Assert.IsFalse(DemoZones.HatchBankBlocked(true, DemoZones.ZoneCount + 1, 1, 0),
                "past the last authored zone the finale is its own beat — no terminal gate");
        }

        [Test]
        public void IsZoneOne_IsT1AndT2Only()
        {
            Assert.IsTrue(FirstTerminal.IsZoneOne("T1"));
            Assert.IsTrue(FirstTerminal.IsZoneOne("T2"));
            Assert.IsFalse(FirstTerminal.IsZoneOne("T3"));
            Assert.IsFalse(FirstTerminal.IsZoneOne(null));
        }

        [Test]
        public void HatchBank_BlockedUntilBothZoneTerminalsDone()
        {
            Assert.IsTrue(FirstTerminal.HatchBankBlocked(true, true, false, true, false));
            Assert.IsTrue(FirstTerminal.HatchBankBlocked(true, true, true, true, false),
                "the second terminal still blocks the bank");
            Assert.IsTrue(FirstTerminal.HatchBankBlocked(true, true, false, true, true));
            Assert.IsFalse(FirstTerminal.HatchBankBlocked(true, true, true, true, true));
        }

        [Test]
        public void HatchBank_AbsentTerminalCannotBlock()
        {
            Assert.IsFalse(FirstTerminal.HatchBankBlocked(true, false, false, false, false));
            Assert.IsFalse(FirstTerminal.HatchBankBlocked(true, true, true, false, false));
        }

        [Test]
        public void OtherField_BlockedUntilFirstClears_NotUntilABankStarts()
        {
            Assert.IsFalse(FirstTerminal.OtherFieldBlocked("T1", true, true, false));
            Assert.IsTrue(FirstTerminal.OtherFieldBlocked("T2", true, true, false));
            Assert.IsFalse(FirstTerminal.OtherFieldBlocked("T2", true, true, true),
                "the zone's bank comes after both terminals, so no bank-started clause");
            Assert.IsFalse(FirstTerminal.OtherFieldBlocked("T2", true, false, false));
        }

        [Test]
        public void DemoOff_NoRuleApplies()
        {
            Assert.IsFalse(FirstTerminal.HatchBankBlocked(false, true, false, true, false));
            Assert.IsFalse(FirstTerminal.OtherFieldBlocked("T2", false, true, false));
        }
    }
}
