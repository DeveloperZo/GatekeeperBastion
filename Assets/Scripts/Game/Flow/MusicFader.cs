using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>Two-source equal-power fade on a music host. Volumes are
    /// <see cref="AudioBuses.MusicMultiplier"/> times the fade mix, written in
    /// LateUpdate so a settings apply cannot stick for more than a frame.</summary>
    public sealed class MusicFader : MonoBehaviour
    {
        #region Constants
        public const float CrossfadeSeconds = 1.5f;
        #endregion

        #region State
        AudioSource _a;
        AudioSource _b;
        AudioSource _from;
        AudioSource _to;
        float _elapsed;
        float _duration;
        float _fromMix = 1f;
        float _toMix = 1f;
        bool _busy;
        bool _destroyOnDone;
        #endregion

        #region Public API
        public static MusicFader Ensure(AudioSource primary)
        {
            if (primary == null) return null;
            var fader = primary.GetComponent<MusicFader>();
            if (fader == null)
                fader = primary.gameObject.AddComponent<MusicFader>();
            fader.Bind(primary);
            return fader;
        }

        public void GoTo(AudioClip clip, float seconds)
        {
            if (clip == null) return;
            Bind(_a);
            AudioSource current = _to != null ? _to : Playing();
            if (current != null && current.clip == clip && current.isPlaying)
            {
                _to = current;
                AudioSource other = Other(current);
                if (other != null && other != current)
                {
                    other.Stop();
                    other.clip = null;
                }
                _from = other;
                _fromMix = 0f;
                _toMix = 1f;
                _busy = false;
                ApplyVolumes();
                return;
            }

            AudioSource dest = current == null ? _a : Other(current);
            if (dest == null) dest = EnsureSecondary();
            dest.clip = clip;
            dest.loop = true;
            dest.spatialBlend = 0f;
            dest.playOnAwake = false;
            dest.volume = 0f;
            dest.Play();

            _from = current;
            _to = dest;
            _fromMix = current != null ? 1f : 0f;
            _toMix = 0f;
            _elapsed = 0f;
            _duration = seconds > 0.01f ? seconds : 0.01f;
            _busy = true;
            _destroyOnDone = false;
        }

        public void FadeOutAndDestroy(float seconds)
        {
            Bind(_a);
            AudioSource playing = Playing();
            _from = playing != null ? playing : _a;
            _to = null;
            _fromMix = 1f;
            _toMix = 0f;
            _elapsed = 0f;
            _duration = seconds > 0.01f ? seconds : 0.01f;
            _busy = true;
            _destroyOnDone = true;
            gameObject.name = "Music Out";
        }
        #endregion

        #region Unity Lifecycle
        void LateUpdate()
        {
            if (_busy)
            {
                _elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(_elapsed / _duration);
                float rad = u * 1.5707963f;
                _fromMix = Mathf.Cos(rad);
                _toMix = Mathf.Sin(rad);
                if (u >= 1f)
                {
                    _busy = false;
                    if (_from != null && _from != _to)
                    {
                        _from.Stop();
                        _from.clip = null;
                    }
                    _fromMix = 0f;
                    _toMix = 1f;
                    if (_destroyOnDone)
                    {
                        Destroy(gameObject);
                        return;
                    }
                }
            }
            ApplyVolumes();
        }
        #endregion

        #region Private
        void Bind(AudioSource primary)
        {
            if (primary == null) return;
            _a = primary;
            if (_b == null)
            {
                var sources = GetComponents<AudioSource>();
                for (int i = 0; i < sources.Length; i++)
                {
                    if (sources[i] != null && sources[i] != _a)
                    {
                        _b = sources[i];
                        break;
                    }
                }
            }
            if (_b == null)
                _b = gameObject.AddComponent<AudioSource>();
            CopyBus(_a, _b);
        }

        AudioSource EnsureSecondary()
        {
            if (_b == null)
                _b = gameObject.AddComponent<AudioSource>();
            if (_a != null)
                CopyBus(_a, _b);
            return _b;
        }

        AudioSource Playing()
        {
            if (_to != null && _to.isPlaying) return _to;
            if (_a != null && _a.isPlaying) return _a;
            if (_b != null && _b.isPlaying) return _b;
            return null;
        }

        AudioSource Other(AudioSource src)
        {
            if (src == _a) return _b != null ? _b : EnsureSecondary();
            return _a;
        }

        void ApplyVolumes()
        {
            float music = AudioBuses.MusicMultiplier;
            if (_a != null)
                _a.volume = MixFor(_a) * music;
            if (_b != null)
                _b.volume = MixFor(_b) * music;
        }

        float MixFor(AudioSource src)
        {
            if (src == _to) return _toMix;
            if (src == _from) return _fromMix;
            return 0f;
        }

        static void CopyBus(AudioSource from, AudioSource to)
        {
            if (from == null || to == null) return;
            to.loop = true;
            to.playOnAwake = false;
            to.spatialBlend = 0f;
            to.dopplerLevel = 0f;
            to.outputAudioMixerGroup = from.outputAudioMixerGroup;
            to.priority = from.priority;
        }
        #endregion
    }
}
