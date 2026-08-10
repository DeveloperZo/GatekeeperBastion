using GatekeeperBastion.Game.Ui;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>One looping front-end track across Splash → MainMenu. Scene
    /// <c>Background Music</c> sources join or yield to the live host; Ship/Bastion
    /// call <see cref="Stop"/> so combat is silent until the menu returns.</summary>
    public static class FrontEndMusic
    {
        const string HostName = "Background Music";

        static AudioSource _live;

        /// <summary>Keep playing through a scene load, or adopt <paramref name="sceneSource"/>
        /// as the persistent host. Scene copies are stopped immediately so PlayOnAwake
        /// cannot restart the track.</summary>
        public static void CaptureOrJoin(AudioSource sceneSource)
        {
            if (_live == null)
                _live = FindLiveHost();

            // Prefer an already-playing host (DDOL or scene). Promote it so scene
            // unload cannot kill the track.
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
            // PlayOnAwake is off on the authored sources — first capture must start
            // playback. Re-Play only when not already playing so join keeps time.
            if (!_live.isPlaying)
                _live.Play();

            SilenceAndDestroy(sceneSource, _live);
            DestroyOthersExcept(_live);
        }

        /// <summary>Stop and destroy the persistent host (leave the front end).</summary>
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
