using System;
using System.Collections.Generic;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Game;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>M24: declaration-driven settings store — defaults, round-trip, restore, unique ids.
    /// In-memory <see cref="ISettingsStore"/> only; never touches real PlayerPrefs.</summary>
    public class SettingsTests
    {
        MemorySettingsStore _store;

        [SetUp]
        public void SetUp()
        {
            _store = new MemorySettingsStore();
            Settings.Bind(_store);
        }

        [TearDown]
        public void TearDown() => Settings.Unbind();

        [Test]
        public void DefaultsMatchDeclarations()
        {
            // Fresh store: every declaration reads its stated default.
            for (int i = 0; i < Settings.Declarations.Length; i++)
            {
                var d = Settings.Declarations[i];
                switch (d.Type)
                {
                    case SettingType.Float:
                        Assert.AreEqual(d.DefaultFloat, Settings.GetFloat(d.Id), 1e-5f,
                            d.Id + " float default");
                        break;
                    case SettingType.Bool:
                        Assert.AreEqual(d.DefaultBool, Settings.GetBool(d.Id),
                            d.Id + " bool default");
                        break;
                    case SettingType.Int:
                        Assert.AreEqual(d.DefaultInt, Settings.GetInt(d.Id),
                            d.Id + " int default");
                        break;
                }
            }
        }

        [Test]
        public void RoundTripsThroughStore()
        {
            Assert.Greater(Settings.Declarations.Length, 0, "need at least one declaration");
            var d = Settings.DeclarationOf(SettingId.MouseSensitivity);
            Assert.AreEqual(SettingType.Float, d.Type, "MouseSensitivity is float");

            float mid = (d.Min + d.Max) * 0.5f;
            Settings.SetFloat(d.Id, mid);
            Assert.AreEqual(mid, Settings.GetFloat(d.Id), 1e-5f);

            // Persist + reload via a new Bind on the same backing store.
            Settings.Unbind();
            Settings.Bind(_store);
            Assert.AreEqual(mid, Settings.GetFloat(d.Id), 1e-5f, "value survived reload");
        }

        [Test]
        public void RestoreDefaultsResetsAll()
        {
            for (int i = 0; i < Settings.Declarations.Length; i++)
            {
                var d = Settings.Declarations[i];
                switch (d.Type)
                {
                    case SettingType.Float:
                        Settings.SetFloat(d.Id, d.Max);
                        Assert.AreNotEqual(d.DefaultFloat, Settings.GetFloat(d.Id));
                        break;
                    case SettingType.Bool:
                        Settings.SetBool(d.Id, !d.DefaultBool);
                        Assert.AreNotEqual(d.DefaultBool, Settings.GetBool(d.Id));
                        break;
                    case SettingType.Int:
                        Settings.SetInt(d.Id, (int)d.Min);
                        Assert.AreNotEqual(d.DefaultInt, Settings.GetInt(d.Id));
                        break;
                }
            }

            Settings.RestoreDefaults();

            for (int i = 0; i < Settings.Declarations.Length; i++)
            {
                var d = Settings.Declarations[i];
                switch (d.Type)
                {
                    case SettingType.Float:
                        Assert.AreEqual(d.DefaultFloat, Settings.GetFloat(d.Id), 1e-5f, d.Id.ToString());
                        break;
                    case SettingType.Bool:
                        Assert.AreEqual(d.DefaultBool, Settings.GetBool(d.Id), d.Id.ToString());
                        break;
                    case SettingType.Int:
                        Assert.AreEqual(d.DefaultInt, Settings.GetInt(d.Id), d.Id.ToString());
                        break;
                }
            }
        }

        [Test]
        public void SettingIdsAreUnique()
        {
            var seen = new HashSet<SettingId>();
            for (int i = 0; i < Settings.Declarations.Length; i++)
            {
                var id = Settings.Declarations[i].Id;
                Assert.IsTrue(seen.Add(id), "duplicate SettingId in Declarations: " + id);
            }
        }

        [Test]
        public void DiscardReloadOnSwapDefaultsOn()
        {
            // M25 criterion 4 — today's behaviour is the default.
            Assert.IsTrue(Settings.GetBool(SettingId.DiscardReloadOnSwap));
        }

        /// <summary>In-memory prefs — same class of isolation M17 applied to the save file.</summary>
        sealed class MemorySettingsStore : ISettingsStore
        {
            readonly Dictionary<string, float> _floats = new Dictionary<string, float>();
            readonly Dictionary<string, int> _ints = new Dictionary<string, int>();

            public float GetFloat(string key, float defaultValue)
                => _floats.TryGetValue(key, out float v) ? v : defaultValue;

            public void SetFloat(string key, float value) => _floats[key] = value;

            public int GetInt(string key, int defaultValue)
                => _ints.TryGetValue(key, out int v) ? v : defaultValue;

            public void SetInt(string key, int value) => _ints[key] = value;

            public void Save() { }
        }
    }
}
