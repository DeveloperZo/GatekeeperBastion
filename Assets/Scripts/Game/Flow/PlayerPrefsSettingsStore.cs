using GatekeeperBastion.Sim;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>Unity adapter for <see cref="Settings"/> — the only code that touches
    /// <c>PlayerPrefs</c> for preferences. Bound at boot; never writes the campaign save.</summary>
    public sealed class PlayerPrefsSettingsStore : ISettingsStore
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Settings.Unbind();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            Settings.Bind(new PlayerPrefsSettingsStore());
            AudioBuses.ApplyFromSettings();
        }

        public float GetFloat(string key, float defaultValue)
            => PlayerPrefs.GetFloat(key, defaultValue);

        public void SetFloat(string key, float value)
            => PlayerPrefs.SetFloat(key, value);

        public int GetInt(string key, int defaultValue)
            => PlayerPrefs.GetInt(key, defaultValue);

        public void SetInt(string key, int value)
            => PlayerPrefs.SetInt(key, value);

        public void Save() => PlayerPrefs.Save();
    }
}
