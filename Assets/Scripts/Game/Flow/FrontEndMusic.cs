using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>One looping Stardust Drifter host across Splash → MainMenu →
    /// RealmSelect. Each scene plays a slice of the same track. Scene
    /// <c>Background Music</c> sources join or yield to the live host.
    /// Continue / Host / Join / realm Enter call <see cref="Stop"/> (1.5 s fade).
    /// Ship/Bastion music is <see cref="LevelMusic"/>.</summary>
    public static class FrontEndMusic
    {
        const string HostName = "Background Music";
        const string SplashClipPath = "Audio/Beds/Stardust-Splash";
        const string MenuClipPath = "Audio/Beds/Stardust-Menu";
        const string RealmClipPath = "Audio/Beds/Stardust-Realm";

        static AudioSource _live;
        static AudioClip _splashClip;
        static AudioClip _menuClip;
        static AudioClip _realmClip;

        /// <summary>Keep playing through a front-end load, or adopt
        /// <paramref name="sceneSource"/> / the authored host. Switches slice
        /// (Splash / Menu / Realm) with a 1.5 s crossfade.</summary>
        public static void CaptureOrJoin(AudioSource sceneSource)
        {
            if (_live == null)
                _live = FindLiveHost();

            AudioSource adopt = sceneSource != null ? sceneSource : _live;
            if (adopt == null)
                adopt = CreateHost();

            EnsurePersistent(adopt);
            _live = adopt;
            SilenceAndDestroy(sceneSource, _live);
            DestroyOthersExcept(_live);

            AudioClip clip = ClipForActiveScene();
            if (clip == null)
                clip = _live.clip;
            var fader = MusicFader.Ensure(_live);
            if (fader != null && clip != null)
                fader.GoTo(clip, MusicFader.CrossfadeSeconds);
            else if (!_live.isPlaying && _live.clip != null)
                _live.Play();
        }

        /// <summary>Fade out and destroy the persistent host (leave the front end).</summary>
        public static void Stop()
        {
            if (_live == null)
                _live = FindLiveHost();
            if (_live == null) return;
            var fader = MusicFader.Ensure(_live);
            _live = null;
            if (fader != null)
                fader.FadeOutAndDestroy(MusicFader.CrossfadeSeconds);
        }

        static AudioClip ClipForActiveScene()
        {
            string n = SceneFlow.ActiveSceneName;
            if (n == SceneFlow.Splash)
            {
                if (_splashClip == null)
                    _splashClip = Resources.Load<AudioClip>(SplashClipPath);
                return _splashClip;
            }
            if (n == SceneFlow.RealmSelect)
            {
                if (_realmClip == null)
                    _realmClip = Resources.Load<AudioClip>(RealmClipPath);
                return _realmClip;
            }
            if (_menuClip == null)
                _menuClip = Resources.Load<AudioClip>(MenuClipPath);
            return _menuClip;
        }

        static AudioSource CreateHost()
        {
            var go = new GameObject(HostName);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 0f;
            return src;
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

        static void DestroyOthersExcept(AudioSource keep)
        {
            var all = Object.FindObjectsByType<AudioSource>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                var src = all[i];
                if (src == null || src == keep) continue;
                if (src.gameObject == keep.gameObject) continue;
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
