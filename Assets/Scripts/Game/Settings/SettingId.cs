namespace GatekeeperBastion.Game
{
    /// <summary>Value shape for one <see cref="SettingDecl"/> row.</summary>
    public enum SettingType
    {
        Bool = 0,
        Float = 1,
        Int = 2,
    }

    /// <summary>Ids for every declared preference. Readers call <see cref="Settings.GetFloat"/> /
    /// <see cref="Settings.GetBool"/> / <see cref="Settings.GetInt"/>; a typo is a compile error —
    /// same role as <c>ControlId</c> for bindings.</summary>
    public enum SettingId
    {
        MouseSensitivity = 0,
        /// <summary>When true (default), switching weapons zeroes the outgoing slot's reload
        /// timer. When false, progress is kept and resumes on return; stowed slots never tick.</summary>
        DiscardReloadOnSwap = 1,
        MasterVolume = 2,
        MusicVolume = 3,
        SfxVolume = 4,
        /// <summary>0 = windowed, 1 = fullscreen. Changing it arms a 15s revert.</summary>
        DisplayMode = 5,
    }
}
