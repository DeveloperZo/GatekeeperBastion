using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>Code-hosted combat one-shots — the audio half of <see cref="Effects"/>. Four
    /// placeholder clips (synthesized, in Resources/Audio/Sfx) at the four feedback moments a
    /// horde shooter is most punished for missing: you hit a body, a body died, the hatch was
    /// struck, you were hurt. Replacing a clip is an asset swap; no code moves.</summary>
    /// <remarks>
    /// Sources are transient GameObjects named so <see cref="AudioBuses"/> classifies them as
    /// SFX, at <see cref="WeaponView.SfxOneShotScale"/> like every other one-shot. Each event
    /// carries a small min-interval so a 40-body wave dying into one grenade reads as one hit,
    /// not a 40-stack ear blast. Missing clips log once and go quiet — never per-event spam.
    /// </remarks>
    public static class CombatSfx
    {
        const string Tag = "CombatSfx";

        const string HitConfirmPath = "Audio/Sfx/Hit-Confirm";
        const string EnemyDownPath = "Audio/Sfx/Enemy-Down";
        const string HatchStruckPath = "Audio/Sfx/Hatch-Struck";
        const string PlayerHurtPath = "Audio/Sfx/Player-Hurt";

        static AudioClip _hitConfirm, _enemyDown, _hatchStruck, _playerHurt;
        static bool _loaded;
        static float _nextHitConfirm, _nextEnemyDown, _nextHatchStruck, _nextPlayerHurt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _hitConfirm = _enemyDown = _hatchStruck = _playerHurt = null;
            _loaded = false;
            _nextHitConfirm = _nextEnemyDown = _nextHatchStruck = _nextPlayerHurt = 0f;
        }

        /// <summary>2D tick on the shooting player: your shot damaged a body. Once per shot
        /// (caller gates on the first damaged body of a ray).</summary>
        public static void HitConfirm()
            => Play2D(Clip(ref _hitConfirm, HitConfirmPath), ref _nextHitConfirm, 0.03f);

        /// <summary>A gatekeeper died here. Paired with the death explosion.</summary>
        public static void EnemyDown(Vector3 position)
            => Play3D(Clip(ref _enemyDown, EnemyDownPath), position, ref _nextEnemyDown, 0.06f);

        /// <summary>The hatch took a hit here. Paired with the hatch damage sparks.</summary>
        public static void HatchStruck(Vector3 position)
            => Play3D(Clip(ref _hatchStruck, HatchStruckPath), position, ref _nextHatchStruck, 0.1f);

        /// <summary>2D: the LOCAL player took damage (caller checks locality).</summary>
        public static void PlayerHurt()
            => Play2D(Clip(ref _playerHurt, PlayerHurtPath), ref _nextPlayerHurt, 0.12f);

        static AudioClip Clip(ref AudioClip slot, string path)
        {
            if (slot != null) return slot;
            if (!_loaded)
            {
                // One load pass for all four; a clip missing after that stays quiet.
                _loaded = true;
                _hitConfirm = Load(HitConfirmPath);
                _enemyDown = Load(EnemyDownPath);
                _hatchStruck = Load(HatchStruckPath);
                _playerHurt = Load(PlayerHurtPath);
            }
            return slot;
        }

        static AudioClip Load(string path)
        {
            var clip = Resources.Load<AudioClip>(path);
            if (clip == null) Log.Warn(Tag, "Missing SFX clip: " + path);
            return clip;
        }

        static void Play2D(AudioClip clip, ref float next, float minInterval)
        {
            if (clip == null || Effects.Suspended) return;
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + minInterval;
            Spawn(clip, Vector3.zero, spatial: 0f);
        }

        static void Play3D(AudioClip clip, Vector3 position, ref float next, float minInterval)
        {
            if (clip == null || Effects.Suspended) return;
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + minInterval;
            Spawn(clip, position, spatial: 1f);
        }

        static void Spawn(AudioClip clip, Vector3 position, float spatial)
        {
            // Name must NOT read as Music — AudioBuses classifies by name.
            var go = new GameObject("SfxOneShot");
            go.transform.position = position;
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = spatial;
            src.minDistance = 2f;
            src.maxDistance = 35f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.volume = AudioBuses.SfxMultiplier;
            src.PlayOneShot(clip, WeaponView.SfxOneShotScale);
            Object.Destroy(go, clip.length + 0.1f);
        }
    }
}
