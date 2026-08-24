using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The scenery copy's life intake: a replicated Hp lands on the mirror without
    /// ever deciding life or death — removal belongs to the despawn message alone.</summary>
    public class TurretLifeMirrorTests
    {
        static TurretState Standing()
        {
            var state = new TurretState();
            state.ResetForRun();
            string reason;
            Assert.IsTrue(state.TryPlace(1f, 0f, 2f, out reason), reason);
            return state;
        }

        [Test]
        public void MirrorHp_TracksTheReplicatedValue()
        {
            var state = Standing();
            state.MirrorHp(state.MaxHp * 0.4f);
            Assert.AreEqual(state.MaxHp * 0.4f, state.Hp, 0.0001f);
            Assert.IsTrue(state.IsLive);
        }

        [Test]
        public void MirrorHp_ClampsToTheMachinesRange()
        {
            var state = Standing();
            state.MirrorHp(state.MaxHp + 500f);
            Assert.AreEqual(state.MaxHp, state.Hp, 0.0001f);
            state.MirrorHp(-25f);
            Assert.AreEqual(0f, state.Hp, 0.0001f);
        }

        [Test]
        public void MirrorHp_NeverFlipsLife_EitherWay()
        {
            var state = Standing();
            // A zero arriving before the despawn message shows an empty bar, not a kill.
            state.MirrorHp(0f);
            Assert.IsTrue(state.IsLive);

            // And onto a cleared machine it lands nowhere — no resurrection by replication.
            state.Clear();
            state.MirrorHp(50f);
            Assert.IsFalse(state.IsLive);
            Assert.AreEqual(0f, state.Hp, 0.0001f);
        }
    }
}
