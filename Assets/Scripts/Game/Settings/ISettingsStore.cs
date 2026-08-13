namespace GatekeeperBastion.Game
{
    /// <summary>Persistence backend for <see cref="Settings"/>. Pure C# so EditMode tests can
    /// bind an in-memory store; the Unity adapter lives under <c>Game/Flow/</c>.</summary>
    public interface ISettingsStore
    {
        float GetFloat(string key, float defaultValue);
        void SetFloat(string key, float value);
        int GetInt(string key, int defaultValue);
        void SetInt(string key, int value);
        void Save();
    }
}
