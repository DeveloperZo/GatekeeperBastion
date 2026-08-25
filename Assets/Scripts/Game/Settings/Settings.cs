using System;

namespace GatekeepersProtocol.Game
{
    /// <summary>Engine-free preferences store: declarations, defaults, current values and
    /// restore-defaults. Persistence goes through <see cref="ISettingsStore"/> — never
    /// <c>PlayerPrefs</c> or <c>gatekeeper_meta.json</c>.</summary>
    public static class Settings
    {
        /// <summary>The one table the options screen iterates. Add a setting by appending a row.</summary>
        public static readonly SettingDecl[] Declarations =
        {
            new SettingDecl(
                SettingId.DisplayMode, "Display mode", SettingCategory.Graphics,
                defaultInt: 1, min: 0, max: 1, prefKey: "gkb_display_mode"),
            new SettingDecl(
                SettingId.MasterVolume, "Master", SettingCategory.Audio,
                defaultFloat: 0.85f, min: 0f, max: 1f, prefKey: "gkb_master_volume"),
            new SettingDecl(
                SettingId.MusicVolume, "Music", SettingCategory.Audio,
                defaultFloat: 0.8f, min: 0f, max: 1f, prefKey: "gkb_music_volume"),
            new SettingDecl(
                SettingId.SfxVolume, "SFX", SettingCategory.Audio,
                defaultFloat: 0.9f, min: 0f, max: 1f, prefKey: "gkb_sfx_volume"),
            // Key string kept from an earlier PlayerPrefs write so existing installs keep their value.
            new SettingDecl(
                SettingId.MouseSensitivity, "Mouse sensitivity", SettingCategory.Controls,
                defaultFloat: 2f, min: 0.2f, max: 8f, prefKey: "gkb_sensitivity"),
            new SettingDecl(
                SettingId.DiscardReloadOnSwap,
                "Discard reload when switching weapons", SettingCategory.Gameplay,
                defaultBool: true,
                prefKey: "gkb_discard_reload_on_swap"),
            new SettingDecl(
                SettingId.ShowComingSoonEntries,
                "Compendium lists Coming Soon entries", SettingCategory.Gameplay,
                defaultBool: true,
                prefKey: "gkb_show_coming_soon"),
            // Defaults FALSE so a first-time player is still taught; finishing that tutorial
            // once turns it on, and every realm opened afterwards starts past it.
            new SettingDecl(
                SettingId.SkipShipTutorial,
                "Skip the ship tutorial on new realms", SettingCategory.Gameplay,
                defaultBool: false,
                prefKey: "gkb_skip_ship_tutorial"),
        };

        static ISettingsStore _store;
        static float[] _floats;
        static bool[] _bools;
        static int[] _ints;

        /// <summary>True after <see cref="Bind"/> and before <see cref="Unbind"/>.</summary>
        public static bool IsBound => _store != null;

        /// <summary>Inject the persistence adapter and load every declared value. Safe to call
        /// again (reloads from the new store).</summary>
        public static void Bind(ISettingsStore store)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            _store = store;
            EnsureBuffers();
            LoadAll();
        }

        /// <summary>Drop the store (domain-reload / test teardown). Readers fall back to defaults.</summary>
        public static void Unbind()
        {
            _store = null;
            _floats = null;
            _bools = null;
            _ints = null;
        }

        public static float GetFloat(SettingId id)
        {
            var d = Require(id, SettingType.Float);
            if (_floats == null) return d.DefaultFloat;
            return _floats[(int)id];
        }

        public static void SetFloat(SettingId id, float value)
        {
            var d = Require(id, SettingType.Float);
            float clamped = value;
            if (clamped < d.Min) clamped = d.Min;
            if (clamped > d.Max) clamped = d.Max;
            EnsureBuffers();
            _floats[(int)id] = clamped;
            if (_store == null) return;
            _store.SetFloat(d.PrefKey, clamped);
            _store.Save();
        }

        public static bool GetBool(SettingId id)
        {
            var d = Require(id, SettingType.Bool);
            if (_bools == null) return d.DefaultBool;
            return _bools[(int)id];
        }

        public static void SetBool(SettingId id, bool value)
        {
            var d = Require(id, SettingType.Bool);
            EnsureBuffers();
            _bools[(int)id] = value;
            if (_store == null) return;
            _store.SetInt(d.PrefKey, value ? 1 : 0);
            _store.Save();
        }

        public static int GetInt(SettingId id)
        {
            var d = Require(id, SettingType.Int);
            if (_ints == null) return d.DefaultInt;
            return _ints[(int)id];
        }

        public static void SetInt(SettingId id, int value)
        {
            var d = Require(id, SettingType.Int);
            int clamped = value;
            int min = (int)d.Min;
            int max = (int)d.Max;
            if (clamped < min) clamped = min;
            if (clamped > max) clamped = max;
            EnsureBuffers();
            _ints[(int)id] = clamped;
            if (_store == null) return;
            _store.SetInt(d.PrefKey, clamped);
            _store.Save();
        }

        /// <summary>Write every declared setting back to its stated default and persist.</summary>
        public static void RestoreDefaults()
        {
            EnsureBuffers();
            for (int i = 0; i < Declarations.Length; i++)
                WriteDefault(Declarations[i]);
            if (_store != null) _store.Save();
        }

        /// <summary>Restore defaults for one options tab only (the per-page Reset).</summary>
        public static void RestoreDefaults(SettingCategory category)
        {
            EnsureBuffers();
            for (int i = 0; i < Declarations.Length; i++)
            {
                if (Declarations[i].Category != category) continue;
                WriteDefault(Declarations[i]);
            }
            if (_store != null) _store.Save();
        }

        public static SettingDecl DeclarationOf(SettingId id)
        {
            for (int i = 0; i < Declarations.Length; i++)
            {
                if (Declarations[i].Id == id) return Declarations[i];
            }
            throw new ArgumentOutOfRangeException(nameof(id), id, "No declaration for SettingId");
        }

        static void WriteDefault(SettingDecl d)
        {
            switch (d.Type)
            {
                case SettingType.Float:
                    _floats[(int)d.Id] = d.DefaultFloat;
                    if (_store != null) _store.SetFloat(d.PrefKey, d.DefaultFloat);
                    break;
                case SettingType.Bool:
                    _bools[(int)d.Id] = d.DefaultBool;
                    if (_store != null) _store.SetInt(d.PrefKey, d.DefaultBool ? 1 : 0);
                    break;
                case SettingType.Int:
                    _ints[(int)d.Id] = d.DefaultInt;
                    if (_store != null) _store.SetInt(d.PrefKey, d.DefaultInt);
                    break;
            }
        }

        static void LoadAll()
        {
            for (int i = 0; i < Declarations.Length; i++)
            {
                var d = Declarations[i];
                switch (d.Type)
                {
                    case SettingType.Float:
                        float v = _store.GetFloat(d.PrefKey, d.DefaultFloat);
                        if (v < d.Min) v = d.Min;
                        if (v > d.Max) v = d.Max;
                        _floats[(int)d.Id] = v;
                        break;
                    case SettingType.Bool:
                        _bools[(int)d.Id] = _store.GetInt(d.PrefKey, d.DefaultBool ? 1 : 0) != 0;
                        break;
                    case SettingType.Int:
                        int iv = _store.GetInt(d.PrefKey, d.DefaultInt);
                        int min = (int)d.Min;
                        int max = (int)d.Max;
                        if (iv < min) iv = min;
                        if (iv > max) iv = max;
                        _ints[(int)d.Id] = iv;
                        break;
                }
            }
        }

        static void EnsureBuffers()
        {
            int n = Enum.GetValues(typeof(SettingId)).Length;
            if (_floats == null || _floats.Length != n) _floats = new float[n];
            if (_bools == null || _bools.Length != n) _bools = new bool[n];
            if (_ints == null || _ints.Length != n) _ints = new int[n];
            if (_store == null)
            {
                for (int i = 0; i < Declarations.Length; i++)
                {
                    var d = Declarations[i];
                    if (d.Type == SettingType.Float) _floats[(int)d.Id] = d.DefaultFloat;
                    else if (d.Type == SettingType.Bool) _bools[(int)d.Id] = d.DefaultBool;
                    else _ints[(int)d.Id] = d.DefaultInt;
                }
            }
        }

        static SettingDecl Require(SettingId id, SettingType type)
        {
            var d = DeclarationOf(id);
            if (d.Type != type)
                throw new InvalidOperationException(
                    $"SettingId.{id} is {d.Type}, not {type}");
            return d;
        }
    }
}
