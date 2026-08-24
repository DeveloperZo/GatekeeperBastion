using UnityEngine;

namespace GatekeepersProtocol.Sim
{
    /// <summary>The pure half of the Cloaker's watch rule: is a body inside a watcher's planar
    /// gaze cone, with a Schmitt release band so the state cannot strobe at the cone's edge.
    /// Line of sight is the Game layer's half (it needs Physics); everything arithmetic is here
    /// so EditMode pins it without a scene. State (who is observed right now) lives in the Game
    /// layer's CloakerGatekeeper — this class holds no state at all.</summary>
    public static class CloakerWatch
    {
        /// <summary>Watched at ≤ the half-angle; released only past this much MORE. The band
        /// exists because edge strobing is angle noise: mouse jitter of a few degrees can never
        /// cross six, while a deliberate look-away crosses the whole band in one poll. A time
        /// delay would also stop the strobe, but it would eat the 0.25 s resume budget.</summary>
        public const float ReleaseAngleSlackDegrees = 6f;

        /// <summary>Same band on range, for a Cloaker walking the range boundary.</summary>
        public const float ReleaseRangeSlackMetres = 2f;

        /// <summary>Planar cone-and-range test. <paramref name="wasObserved"/> selects the
        /// release thresholds (entry + slack) so an already-held Cloaker stays held until the
        /// gaze leaves by more than the slack. Vertical components are discarded: yaw is the
        /// look, pitch lives on a camera child the rule never sees.</summary>
        /// <param name="watcherForward">The watcher's forward (any y; flattened here).</param>
        /// <param name="watcherToBody">Watcher's eye to the body (any y; flattened here).</param>
        public static bool Watched(Vector3 watcherForward, Vector3 watcherToBody,
                                   bool wasObserved, float halfAngleDegrees, float rangeMetres)
        {
            watcherForward.y = 0f;
            watcherToBody.y = 0f;
            if (watcherForward.sqrMagnitude < 0.0001f) return false;
            if (watcherToBody.sqrMagnitude < 0.0001f) return true; // standing on it is seeing it
            float angleLimit = wasObserved ? halfAngleDegrees + ReleaseAngleSlackDegrees
                                           : halfAngleDegrees;
            float rangeLimit = wasObserved ? rangeMetres + ReleaseRangeSlackMetres : rangeMetres;
            if (watcherToBody.magnitude > rangeLimit) return false;
            return Vector3.Angle(watcherForward, watcherToBody) <= angleLimit;
        }
    }
}
