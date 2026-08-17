using System;
using System.IO;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Whispers of the Void hosts on Ship and Bastion, plus SFX start-click
    /// fades — YAML / PCM on disk, no scene load.</summary>
    public class SessionMusicHostTests
    {
        const string WhispersGuid = "482f8aeab93ef914d92d74c5ed0ad611";
        const string StardustGuid = "baf9676f5db601447905dbe89a258d66";
        const float MaxFirstSample = 0.02f;

        [Test]
        public void ShipAndBastion_HostWhispersAsLevelMusic()
        {
            AssertWhispersHost("Assets/Scenes/Ship.unity");
            AssertWhispersHost("Assets/Scenes/Bastion.unity");
        }

        [Test]
        public void SplashAndMainMenu_HostStardustAsBackgroundMusic()
        {
            AssertStardustHost("Assets/Scenes/Splash.unity");
            AssertStardustHost("Assets/Scenes/MainMenu.unity");
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
            Assert.IsTrue(File.Exists(Path.GetFullPath("Assets/Resources/Audio/Stardust Drifter.mp3")));
            Assert.IsTrue(File.Exists(Path.GetFullPath("Assets/Resources/Audio/Whispers of the Void.mp3")));
        }

        [Test]
        public void ReloadAndFootstep_FirstSampleNearSilence()
        {
            AssertFirstNearSilence("Assets/Resources/Audio/Sfx/Reload.wav");
            AssertFirstNearSilence("Assets/Resources/Audio/Sfx/Footstep-Walk.wav");
        }

        static void AssertWhispersHost(string scenePath)
        {
            string yaml = Read(scenePath);
            Assert.IsTrue(yaml.Contains("m_Name: Level Music"), scenePath + " missing Level Music");
            Assert.IsTrue(yaml.Contains("guid: " + WhispersGuid),
                scenePath + " is not bound to Whispers of the Void");
            Assert.IsFalse(yaml.Contains("m_Name: Background Music"),
                scenePath + " still has a FrontEndMusic host");
        }

        static void AssertStardustHost(string scenePath)
        {
            string yaml = Read(scenePath);
            Assert.IsTrue(yaml.Contains("m_Name: Background Music"), scenePath + " missing Background Music");
            Assert.IsTrue(yaml.Contains("guid: " + StardustGuid),
                scenePath + " is not bound to Stardust Drifter");
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
