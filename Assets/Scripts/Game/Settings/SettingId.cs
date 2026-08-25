namespace GatekeepersProtocol.Game
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
        /// <summary>When true (default), the pause Compendium lists Coming Soon entries —
        /// pages for designed-but-unshipped things. When false the reader shows only what
        /// is playable today.</summary>
        ShowComingSoonEntries = 6,
        /// <summary>When true, a realm opens with the ship tutorial already behind it. Set
        /// automatically the first time that tutorial is finished: it teaches where the
        /// stations are, what an interact does and how firing feels, and a player needs that
        /// once — but its flags live on the realm, so every new world would otherwise teach
        /// it again. Untick to be taught again on the next realm.</summary>
        SkipShipTutorial = 7,
    }
}
