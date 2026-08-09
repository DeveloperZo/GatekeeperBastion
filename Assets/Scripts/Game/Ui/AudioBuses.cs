using GatekeeperBastion.Sim;
using UnityEngine;

namespace GatekeeperBastion.Game.Ui
{
    /// <summary>Master / Music / SFX volume apply without FMOD. Master drives
    /// <see cref="AudioListener.volume"/>; Music/SFX scale matching <see cref="AudioSource"/>s
    /// or keep multipliers when none exist yet.</summary>
    public static class AudioBuses
    {
        static float _music = 0.8f;
        static float _sfx = 1f;

        /// <summary>Last applied Music slider (0–1). Used when no Music sources are present.</summary>
        public static float MusicMultiplier => _music;

        /// <summary>Last applied SFX slider (0–1). Used when no SFX sources are present.</summary>
        public static float SfxMultiplier => _sfx;

        /// <summary>Read current Settings and push to listener + live sources.</summary>
        public static void ApplyFromSettings()
        {
            Apply(
                Settings.GetFloat(SettingId.MasterVolume),
                Settings.GetFloat(SettingId.MusicVolume),
                Settings.GetFloat(SettingId.SfxVolume));
        }

        /// <summary>Apply Master to the listener; Music/SFX to classified AudioSources.</summary>
        public static void Apply(float master, float music, float sfx)
        {
            AudioListener.volume = Mathf.Clamp01(master);
            _music = Mathf.Clamp01(music);
            _sfx = Mathf.Clamp01(sfx);

            var sources = Object.FindObjectsByType<AudioSource>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < sources.Length; i++)
            {
                var src = sources[i];
                if (src == null) continue;
                src.volume = IsMusic(src) ? _music : _sfx;
            }
        }

        static bool IsMusic(AudioSource src)
        {
            var go = src.gameObject;
            string name = go.name;
            if (name.IndexOf("Music", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            // Optional Music layer — absent in TagManager today; name match covers Shift BG music.
            int layer = go.layer;
            string layerName = layer >= 0 && layer < 32 ? LayerMask.LayerToName(layer) : null;
            return !string.IsNullOrEmpty(layerName)
                   && layerName.IndexOf("Music", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
