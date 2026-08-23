using System;
using System.IO;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Hydrodynamics beds on Ship/Bastion/Range, Stardust slices on
    /// Splash/MainMenu, plus SFX start-click fades — YAML / PCM on disk, no scene load.</summary>
    public class SessionMusicHostTests
    {
        const string HydroShipGuid = "3bd1c2ee836c46b8a8fe6f8c233e5ae0";
        const string HydroCombatGuid = "453afb1138044a779a326d8b645307fb";
        const string StardustSplashGuid = "9613e068e24e4a7593e67b62325d7661";
        const string StardustMenuGuid = "d82376462b3a451dad39b380c35a22e0";
        const string StardustRealmGuid = "97d9e3b2a38743d680c38cc26298cf48";
        const float MaxFirstSample = 0.02f;

        [Test]
        public void ShipAndRange_HostHydroShipAsLevelMusic()
        {
            AssertLevelHost("Assets/Scenes/Ship - Demo.unity", HydroShipGuid);
            AssertLevelHost("Assets/Scenes/Ship - CoOp.unity", HydroShipGuid);
            AssertLevelHost("Assets/Scenes/Range.unity", HydroShipGuid);
        }

        [Test]
        public void Bastions_HostHydroCombatAsLevelMusic()
        {
            AssertLevelHost("Assets/Scenes/Bastion.unity", HydroCombatGuid);
            AssertLevelHost("Assets/Scenes/Bastion_Coop.unity", HydroCombatGuid);
        }

        [Test]
        public void SplashAndMainMenu_HostStardustSlicesAsBackgroundMusic()
        {
            AssertFrontEndHost("Assets/Scenes/Splash.unity", StardustSplashGuid);
            AssertFrontEndHost("Assets/Scenes/MainMenu.unity", StardustMenuGuid);
            AssertFrontEndHost("Assets/Scenes/RealmSelect.unity", StardustRealmGuid);
        }

        [Test]
        public void BoundSfx_ExistOnDisk()
        {
            string[] names =
            {
                "Fire-Pistol.wav", "Fire-Rifle.wav", "Fire-Shotgun.wav",
                "Reload.wav", "Footstep-Walk.wav"
            };
            for (int i = 0; i < names.Length; i++)
            {
                string path = Path.GetFullPath("Assets/Resources/Audio/Sfx/" + names[i]);
                Assert.IsTrue(File.Exists(path), "missing " + path);
            }
            Assert.IsTrue(File.Exists(Path.GetFullPath("Assets/Resources/Audio/Beds/Stardust-Splash.mp3")));
            Assert.IsTrue(File.Exists(Path.GetFullPath("Assets/Resources/Audio/Beds/Stardust-Menu.mp3")));
            Assert.IsTrue(File.Exists(Path.GetFullPath("Assets/Resources/Audio/Beds/Stardust-Realm.mp3")));
            Assert.IsTrue(File.Exists(Path.GetFullPath("Assets/Resources/Audio/Beds/Hydro-Ship.mp3")));
            Assert.IsTrue(File.Exists(Path.GetFullPath("Assets/Resources/Audio/Beds/Hydro-Combat.mp3")));
        }

        [Test]
        public void ReloadAndFootstep_FirstSampleNearSilence()
        {
            AssertFirstNearSilence("Assets/Resources/Audio/Sfx/Reload.wav");
            AssertFirstNearSilence("Assets/Resources/Audio/Sfx/Footstep-Walk.wav");
        }

        static void AssertLevelHost(string scenePath, string clipGuid)
        {
            string yaml = Read(scenePath);
            Assert.IsTrue(yaml.Contains("m_Name: Level Music"), scenePath + " missing Level Music");
            Assert.IsTrue(yaml.Contains("guid: " + clipGuid),
                scenePath + " is not bound to the Hydrodynamics bed");
            Assert.IsFalse(yaml.Contains("m_Name: Background Music"),
                scenePath + " still has a FrontEndMusic host");
        }

        static void AssertFrontEndHost(string scenePath, string clipGuid)
        {
            string yaml = Read(scenePath);
            Assert.IsTrue(yaml.Contains("m_Name: Background Music"), scenePath + " missing Background Music");
            Assert.IsTrue(yaml.Contains("guid: " + clipGuid),
                scenePath + " is not bound to the Stardust slice");
        }

        static void AssertFirstNearSilence(string relative)
        {
            string path = Path.GetFullPath(relative);
            float sample = FirstPcm16Sample(path);
            Assert.Less(Math.Abs(sample), MaxFirstSample,
                relative + " first sample " + sample + " is a start click");
        }

        static float FirstPcm16Sample(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            int data = IndexOfAscii(bytes, "data");
            Assert.GreaterOrEqual(data, 0, path + " has no data chunk");
            int payload = data + 8;
            Assert.Greater(bytes.Length, payload + 1, path + " data chunk empty");
            short s = (short)(bytes[payload] | (bytes[payload + 1] << 8));
            return s / 32768f;
        }

        static int IndexOfAscii(byte[] bytes, string ascii)
        {
            byte[] needle = System.Text.Encoding.ASCII.GetBytes(ascii);
            for (int i = 0; i <= bytes.Length - needle.Length; i++)
            {
                bool ok = true;
                for (int j = 0; j < needle.Length; j++)
                {
                    if (bytes[i + j] != needle[j]) { ok = false; break; }
                }
                if (ok) return i;
            }
            return -1;
        }

        static string Read(string relative)
        {
            string path = Path.GetFullPath(relative);
            Assert.IsTrue(File.Exists(path), "missing " + path);
            return File.ReadAllText(path);
        }
    }
}
