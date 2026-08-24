using GatekeepersProtocol.Sim;
using NUnit.Framework;
using UnityEngine;

namespace GatekeepersProtocol.Tests
{
    /// <summary>
    /// The pure half of the Cloaker's watch rule (CloakerWatch.Watched): planar cone, planar
    /// range, and the Schmitt release band that stops the observed state strobing at the cone's
    /// edge. Line of sight is the Game layer's half and is exercised in PlayMode.
    ///
    /// The numbers pinned here are the design: entry at 30° / 26 m, release only past 36° /
    /// 28 m. The band is angular, not temporal, because edge strobing is angle noise — mouse
    /// jitter of a few degrees can never cross a 6° band, while a deliberate look-away crosses
    /// the whole band in one 0.1 s poll, which is what keeps both directions of the freeze
    /// inside the 0.25 s criterion.
    /// </summary>
    public class CloakerWatchRuleTests
    {
        const float HalfAngle = 30f;
        const float Range = 26f;

        static Vector3 AtAngle(float degrees, float distance)
        {
            float rad = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * distance;
        }

        [Test]
        public void Watched_InsideConeAndRange_IsTrue()
        {
            Assert.IsTrue(CloakerWatch.Watched(Vector3.forward, AtAngle(0f, 10f),
                wasObserved: false, HalfAngle, Range), "dead centre");
            Assert.IsTrue(CloakerWatch.Watched(Vector3.forward, AtAngle(29f, 25f),
                wasObserved: false, HalfAngle, Range), "just inside both limits");
            Assert.IsTrue(CloakerWatch.Watched(Vector3.forward, AtAngle(-29f, 25f),
                wasObserved: false, HalfAngle, Range), "the cone is symmetric");
        }

        [Test]
        public void Watched_OutsideConeOrRange_IsFalse()
        {
            Assert.IsFalse(CloakerWatch.Watched(Vector3.forward, AtAngle(31f, 10f),
                wasObserved: false, HalfAngle, Range), "past the half-angle");
            Assert.IsFalse(CloakerWatch.Watched(Vector3.forward, AtAngle(0f, 26.5f),
                wasObserved: false, HalfAngle, Range), "past the range");
            Assert.IsFalse(CloakerWatch.Watched(Vector3.forward, AtAngle(180f, 5f),
                wasObserved: false, HalfAngle, Range), "behind the watcher");
        }

        [Test]
        public void Watched_ReleaseBand_HoldsAnAlreadyObservedCloaker()
        {
            // 33° is outside the 30° entry cone but inside the 36° release cone: a fresh
            // Cloaker there is free, an already-held one stays held. This asymmetry IS the
            // no-strobe guarantee of the milestone's criterion 3.
            Assert.IsFalse(CloakerWatch.Watched(Vector3.forward, AtAngle(33f, 10f),
                wasObserved: false, HalfAngle, Range), "not captured at 33°");
            Assert.IsTrue(CloakerWatch.Watched(Vector3.forward, AtAngle(33f, 10f),
                wasObserved: true, HalfAngle, Range), "still held at 33°");
            Assert.IsFalse(CloakerWatch.Watched(Vector3.forward, AtAngle(36.5f, 10f),
                wasObserved: true, HalfAngle, Range), "released past 36°");
        }

        [Test]
        public void Watched_ReleaseBand_AppliesToRangeToo()
        {
            Assert.IsFalse(CloakerWatch.Watched(Vector3.forward, AtAngle(0f, 27f),
                wasObserved: false, HalfAngle, Range), "not captured at 27 m");
            Assert.IsTrue(CloakerWatch.Watched(Vector3.forward, AtAngle(0f, 27f),
                wasObserved: true, HalfAngle, Range), "still held at 27 m");
            Assert.IsFalse(CloakerWatch.Watched(Vector3.forward, AtAngle(0f, 28.5f),
                wasObserved: true, HalfAngle, Range), "released past 28 m");
        }

        [Test]
        public void Watched_IsPlanar_PitchNeverMatters()
        {
            // Yaw is the look; pitch lives on a camera child the rule never sees. A watcher
            // aiming steeply down at the floor still holds the Cloaker on their azimuth, and
            // height difference alone (a body one deck up) neither grants nor breaks the cone.
            Vector3 steepDown = (Vector3.forward + Vector3.down * 5f);
            Assert.IsTrue(CloakerWatch.Watched(steepDown, AtAngle(10f, 10f),
                wasObserved: false, HalfAngle, Range), "pitch on the forward is discarded");
            Vector3 bodyAbove = AtAngle(10f, 10f) + Vector3.up * 8f;
            Assert.IsTrue(CloakerWatch.Watched(Vector3.forward, bodyAbove,
                wasObserved: false, HalfAngle, Range),
                "height difference is discarded — line of sight, not the cone, answers decks");
        }

        [Test]
        public void Watched_DegenerateVectors_AnswerSafely()
        {
            Assert.IsFalse(CloakerWatch.Watched(Vector3.zero, AtAngle(0f, 5f),
                wasObserved: false, HalfAngle, Range), "no forward = no gaze");
            Assert.IsTrue(CloakerWatch.Watched(Vector3.forward, Vector3.zero,
                wasObserved: false, HalfAngle, Range), "standing on it is seeing it");
        }
    }
}
