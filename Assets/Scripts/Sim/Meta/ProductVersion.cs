namespace GatekeepersProtocol.Sim
{
    /// <summary>Stamped product version for the menu strip and player-build folder.
    /// Bump <see cref="Build"/> on each Windows player zip. Bump <see cref="Release"/>
    /// when Zo names a new version (keep PlayerSettings <c>bundleVersion</c> in lockstep).</summary>
    public static class ProductVersion
    {
        public const string Release = "0.34";

        /// <summary>Monotonic player-build counter. Not incremented on every code change.</summary>
        public const int Build = 2;

        public static string Display => Release + "." + Build;
    }
}
