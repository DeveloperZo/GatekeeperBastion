using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: the screen tells you which protocol window is running. Every active
    /// ability is otherwise a rule with no body — a fire-rate multiplier or a targeting-list
    /// removal that the player has no way to see. This is the tell.
    /// <para>Built entirely at run time as a global <see cref="PostProcessVolume"/> whose weight
    /// is blended, never a scene edit. The scenes already carry a <see cref="PostProcessLayer"/>,
    /// and the HUD canvas is Screen Space Overlay, so the readout stays crisp while the world
    /// behind it does not.</para>
    /// <para><b>Only Cloak drains colour.</b> Cloak is a disengage — you are off every targeting
    /// list and not meant to be fighting — so the world can go somewhere else. Every other
    /// window lands mid-fight, where taking saturation or light away would be hostile: those get
    /// an edge wash in their own colour and nothing more.</para></summary>
    [RequireComponent(typeof(PlayerProtocolAbilities))]
    public sealed class ProtocolLayerView : MonoBehaviour
    {
        const string Tag = "Protocol";

        /// <summary>One ability's screen treatment. Presentation only — nothing in Sim reads
        /// these, which is why they are here and not in the catalog.</summary>
        readonly struct Look
        {
            public readonly float Saturation;
            public readonly float Exposure;
            public readonly Color Filter;
            public readonly float Vignette;
            public readonly Color VignetteTint;
            public readonly float Aberration;
            public readonly float Grain;
            public readonly float FadeIn;
            public readonly float FadeOut;

            public Look(float saturation, float exposure, Color filter, float vignette,
                Color vignetteTint, float aberration, float grain, float fadeIn, float fadeOut)
            {
                Saturation = saturation;
                Exposure = exposure;
                Filter = filter;
                Vignette = vignette;
                VignetteTint = vignetteTint;
                Aberration = aberration;
                Grain = grain;
                FadeIn = fadeIn;
                FadeOut = fadeOut;
            }

            public bool IsNothing => Vignette <= 0f && Saturation >= 0f && Aberration <= 0f;
        }

        static readonly Look None = new Look(0f, 0f, Color.white, 0f, Color.black, 0f, 0f, 0.1f, 0.1f);

        /// <summary>VEIL Cloak — the world goes sideways. Heavy desaturation, a violet cast and
        /// a closing edge: you have stepped out of the room, not turned invisible inside it.
        /// Fade out is the quickest of any look because firing exits the layer instantly and a
        /// slow recovery would read as lag rather than release.</summary>
        static readonly Look Cloak = new Look(
            -80f, -0.5f, new Color(0.62f, 0.55f, 0.85f), 0.52f,
            new Color(0.15f, 0.05f, 0.25f), 0.40f, 0.30f, 0.15f, 0.10f);

        /// <summary>VEIL Ping — cold and wide. You have lit the room for the crew and painted
        /// yourself doing it.</summary>
        static readonly Look Ping = new Look(
            -15f, 0f, new Color(0.72f, 0.92f, 1f), 0.34f,
            new Color(0.06f, 0.28f, 0.38f), 0.12f, 0f, 0.12f, 0.30f);

        /// <summary>Default Rush — warm and fast, and deliberately the lightest of the set: it
        /// is the ability you spend the most seconds inside.</summary>
        static readonly Look Rush = new Look(
            8f, 0.05f, new Color(1f, 0.88f, 0.72f), 0.28f,
            new Color(0.32f, 0.14f, 0.03f), 0f, 0f, 0.10f, 0.25f);

        /// <summary>Default Barrage — the planted zone's own orange, brought to the edges so the
        /// caster sees it without looking down at the disc.</summary>
        static readonly Look Barrage = new Look(
            5f, 0f, new Color(1f, 0.84f, 0.66f), 0.32f,
            new Color(0.38f, 0.13f, 0.02f), 0f, 0f, 0.12f, 0.25f);

        /// <summary>FORGE Overcharge — the turret's redline colour, on your screen, because the
        /// machine may be behind you while it burns.</summary>
        static readonly Look Overcharge = new Look(
            10f, 0.05f, new Color(1f, 0.78f, 0.6f), 0.38f,
            new Color(0.46f, 0.11f, 0.02f), 0.18f, 0.10f, 0.10f, 0.25f);

        PlayerProtocolAbilities _abilities;
        PostProcessVolume _volume;
        PostProcessProfile _profile;
        ColorGrading _grading;
        Vignette _vignette;
        ChromaticAberration _aberration;
        Grain _grain;

        BoarderAbilityId _shown = BoarderAbilityId.None;
        Look _look = None;
        float _weight;
        bool _unavailable;

        /// <summary>0 when the world is normal, 1 at full treatment.</summary>
        public float LayerWeight => _weight;

        /// <summary>Which ability the screen is currently showing. None when clear.</summary>
        public BoarderAbilityId Shown => _shown;

        void Awake()
        {
            _abilities = GetComponent<PlayerProtocolAbilities>();
        }

        static Look LookFor(BoarderAbilityId id)
        {
            switch (id)
            {
                case BoarderAbilityId.Cloak: return Cloak;
                case BoarderAbilityId.Ping: return Ping;
                case BoarderAbilityId.Rush: return Rush;
                case BoarderAbilityId.Barrage: return Barrage;
                case BoarderAbilityId.Overcharge: return Overcharge;
                // Field Deploy opens no window and needs none: the turret standing in the room
                // is a better tell than anything the screen could say.
                default: return None;
            }
        }

        void LateUpdate()
        {
            if (_abilities == null || _unavailable) return;

            var active = _abilities.ActiveWindowAbility;
            var look = LookFor(active);
            bool wanted = active != BoarderAbilityId.None && !look.IsNothing;

            // A new window while one is already up: hand over rather than cross-fade, or the
            // two washes average into a colour that belongs to neither.
            if (wanted && active != _shown)
            {
                _shown = active;
                _look = look;
                if (_volume != null) ApplyLook(_look);
            }

            float target = wanted ? 1f : 0f;
            float span = wanted ? _look.FadeIn : _look.FadeOut;
            _weight = span <= 0f
                ? target
                : Mathf.MoveTowards(_weight, target, Time.deltaTime / span);

            if (_weight <= 0f)
            {
                _shown = BoarderAbilityId.None;
                Teardown();
                return;
            }
            if (_volume == null && !TryBuild()) return;
            _volume.weight = _weight;
        }

        /// <summary>Stand the volume up on first use. False when there is no local camera with a
        /// post-process layer to answer to — a headless test rig, or a remote avatar.</summary>
        bool TryBuild()
        {
            var layer = ResolveLayer();
            if (layer == null)
            {
                // Not an error: a rig without a post-process camera simply gets no screen tell,
                // and every ability still works. Latched, because LateUpdate would say it
                // every frame otherwise.
                _unavailable = true;
                Log.Info(Tag, "protocol screen tell off — no PostProcessLayer on the local camera");
                return false;
            }

            _profile = ScriptableObject.CreateInstance<PostProcessProfile>();
            _profile.hideFlags = HideFlags.HideAndDontSave;
            _grading = _profile.AddSettings<ColorGrading>();
            _vignette = _profile.AddSettings<Vignette>();
            _aberration = _profile.AddSettings<ChromaticAberration>();
            _grain = _profile.AddSettings<Grain>();
            ApplyLook(_look);

            var go = new GameObject("ProtocolLayer");
            go.transform.SetParent(transform, false);
            // The camera only sees volumes on the layers its own mask names; asking the camera
            // beats hardcoding a number that a project re-layering would silently break.
            go.layer = FirstLayerIn(layer.volumeLayer.value);

            _volume = go.AddComponent<PostProcessVolume>();
            _volume.isGlobal = true;
            _volume.priority = 100f;   // above whatever ambient grade the scene is running
            _volume.weight = 0f;
            _volume.profile = _profile;
            return true;
        }

        void ApplyLook(Look look)
        {
            if (_profile == null) return;

            // Stated rather than inherited: postExposure is only honoured by the HDR grading
            // path, and the fight cameras are HDR. Left to the default this look could darken
            // on one camera and not another with nothing to show why.
            _grading.gradingMode.overrideState = true;
            _grading.gradingMode.value = GradingMode.HighDefinitionRange;

            _grading.saturation.overrideState = true;
            _grading.saturation.value = look.Saturation;
            _grading.postExposure.overrideState = true;
            _grading.postExposure.value = look.Exposure;
            _grading.colorFilter.overrideState = true;
            _grading.colorFilter.value = look.Filter;

            _vignette.intensity.overrideState = true;
            _vignette.intensity.value = look.Vignette;
            _vignette.smoothness.overrideState = true;
            _vignette.smoothness.value = 0.55f;
            _vignette.color.overrideState = true;
            _vignette.color.value = look.VignetteTint;
            _vignette.rounded.overrideState = true;
            _vignette.rounded.value = true;

            _aberration.intensity.overrideState = true;
            _aberration.intensity.value = look.Aberration;

            _grain.intensity.overrideState = true;
            _grain.intensity.value = look.Grain;
            _grain.size.overrideState = true;
            _grain.size.value = 1.4f;
            _grain.colored.overrideState = true;
            _grain.colored.value = false;
        }

        /// <summary>The post-process layer driving the local view, or null. Searched down this
        /// rig only. The tagged main-camera shortcut is deliberately not used: it is a scene
        /// lookup, and it would hand a remote avatar the local player's camera.</summary>
        PostProcessLayer ResolveLayer()
        {
            var cam = GetComponentInChildren<Camera>(true);
            return cam != null ? cam.GetComponent<PostProcessLayer>() : null;
        }

        /// <summary>Lowest layer index named by a mask, or 0. A volume on any other layer is
        /// invisible to the camera, which reads as the effect silently not working.</summary>
        static int FirstLayerIn(int mask)
        {
            for (int i = 0; i < 32; i++)
                if ((mask & (1 << i)) != 0) return i;
            return 0;
        }

        void Teardown()
        {
            if (_volume != null) Destroy(_volume.gameObject);
            _volume = null;
            // The profile is a runtime ScriptableObject: nothing else owns it, so nothing else
            // will collect it.
            if (_profile != null) Destroy(_profile);
            _profile = null;
            _grading = null;
            _vignette = null;
            _aberration = null;
            _grain = null;
        }

        void OnDisable()
        {
            _weight = 0f;
            _shown = BoarderAbilityId.None;
            Teardown();
        }
    }
}
