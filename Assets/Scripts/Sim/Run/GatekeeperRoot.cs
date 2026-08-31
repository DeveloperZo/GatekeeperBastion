namespace GatekeepersProtocol.Sim
{
    /// <summary>The garrison's first status. A rooted body holds position and keeps its
    /// target; a body already in reach still swings. Not a speed multiplier — anything that
    /// writes agent speed to zero fights Anchor's slow and every RecomputeSpeed caller.
    /// </summary>
    public static class GatekeeperRoot
    {
        /// <summary>False while rooted: the body may not close. In-reach swinging is
        /// <see cref="MaySwing"/>, a separate question, so a rooted body already on the
        /// Boarder still finishes the swing.</summary>
        public static bool MayAdvance(bool rooted)
        {
            return !rooted;
        }

        /// <summary>Root does not cancel a swing. Reach is the only gate.</summary>
        public static bool MaySwing(bool inReach)
        {
            return inReach;
        }
    }
}
