using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>One looping Hydrodynamics host across Ship → Bastion. Scene
    /// <c>Level Music</c> sources join or yield to the live host. Name is not
    /// <c>Background Music</c> — FrontEndMusic would steal that. Ship and Range
    /// play the dark pad bed; Bastion plays the brighter combat bed. Stop when
    /// returning to MainMenu so Stardust and this track never sit at full
    /// overlap past the crossfade.</summary>
    public static class LevelMusic
    {
        const string HostName = "Level Music";
        const string ShipClipPath = "Audio/Beds/Hydro-Ship";
        const string CombatClipPath = "Audio/Beds/Hydro-Combat";

        static AudioSource _live;
        static AudioClip _shipClip;
        static AudioClip _combatClip;

        /// <summary>Keep playing through a Ship/Bastion load, or adopt
        /// <paramref name="sceneSource"/> / the authored host as the persistent source.
        /// Switches bed (Ship vs Combat) with a 1.5 s crossfade.</summary>
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

        /// <summary>Fade out and destroy the persistent host (leave Ship/Bastion).</summary>
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
            bool shipBed = SceneFlow.IsShipHub(n) || n == SceneFlow.Range;
            if (shipBed)
            {
                if (_shipClip == null)
                    _shipClip = Resources.Load<AudioClip>(ShipClipPath);
                return _shipClip;
            }
            if (_combatClip == null)
                _combatClip = Resources.Load<AudioClip>(CombatClipPath);
            return _combatClip;
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
