using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>One looping Whispers of the Void host across Ship → Bastion. Scene
    /// <c>Level Music</c> sources join or yield to the live host. Name is not
    /// <c>Background Music</c> — FrontEndMusic would steal that. Stop when returning
    /// to MainMenu so Stardust and this track never overlap.</summary>
    public static class LevelMusic
    {
        const string HostName = "Level Music";

        static AudioSource _live;

        /// <summary>Keep playing through a Ship/Bastion load, or adopt
        /// <paramref name="sceneSource"/> / the authored host as the persistent source.</summary>
        public static void CaptureOrJoin(AudioSource sceneSource)
        {
            if (_live == null)
                _live = FindLiveHost();

            if (_live != null && _live.isPlaying)
            {
                EnsurePersistent(_live);
                SilenceAndDestroy(sceneSource, _live);
                DestroyOthersExcept(_live);
                ApplyVolume();
                return;
            }

            AudioSource adopt = sceneSource != null ? sceneSource : _live;
            if (adopt == null) return;

            EnsurePersistent(adopt);
            adopt.loop = true;
            adopt.spatialBlend = 0f;
            adopt.playOnAwake = false;
            _live = adopt;
            ApplyVolume();
            if (!_live.isPlaying)
                _live.Play();

            SilenceAndDestroy(sceneSource, _live);
            DestroyOthersExcept(_live);
        }

        /// <summary>Stop and destroy the persistent host (leave Ship/Bastion).</summary>
        public static void Stop()
        {
            if (_live == null)
                _live = FindLiveHost();
            if (_live == null) return;
            var go = _live.gameObject;
            _live.Stop();
            _live = null;
            Object.Destroy(go);
        }

        static void EnsurePersistent(AudioSource src)
        {
            if (src == null) return;
            var go = src.gameObject;
            go.name = HostName;
            if (go.transform.parent != null)
                go.transform.SetParent(null);
            Object.DontDestroyOnLoad(go);
            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 0f;
        }

        static void SilenceAndDestroy(AudioSource src, AudioSource keep)
        {
            if (src == null || src == keep) return;
            src.playOnAwake = false;
            src.Stop();
            src.enabled = false;
            Object.Destroy(src.gameObject);
        }

        static void ApplyVolume()
        {
            AudioBuses.ApplyFromSettings();
            if (_live != null)
                _live.volume = AudioBuses.MusicMultiplier;
        }

        static void DestroyOthersExcept(AudioSource keep)
        {
            var all = Object.FindObjectsByType<AudioSource>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                var src = all[i];
                if (src == null || src == keep) continue;
                if (src.gameObject.name != HostName) continue;
                SilenceAndDestroy(src, keep);
            }
        }

        static AudioSource FindLiveHost()
        {
            AudioSource sceneLocal = null;
            var all = Object.FindObjectsByType<AudioSource>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                var src = all[i];
                if (src == null || src.gameObject.name != HostName) continue;
                string sceneName = src.gameObject.scene.name;
                if (string.IsNullOrEmpty(sceneName) || sceneName == "DontDestroyOnLoad")
                    return src;
                if (sceneLocal == null)
                    sceneLocal = src;
            }
            return sceneLocal;
        }
    }
}
