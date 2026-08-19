using GatekeepersProtocol.Sim;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Zone 1's shape (Docs/Demo/1-first-terminal.md): two terminals, one Wave each,
    /// the second waits on the first, the Hatch waits on both, and none of it applies once
    /// the demo is off.</summary>
    public class FirstTerminalTests
    {
        [Test]
        public void WavesFor_ZoneOneIsOneWavePerTrigger()
        {
            Assert.AreEqual(1, FirstTerminal.WavesFor("T1"));
            Assert.AreEqual(1, FirstTerminal.WavesFor("T2"));
            Assert.AreEqual(1, FirstTerminal.WavesFor("T3"));
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
