using GatekeepersProtocol.Sim;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>The turret's body and the crystal drive that runs on it.
    /// <para>The machine is a pylon: a dark plinth that turns to face whatever the gun is
    /// shooting, and a crystal floating clear of it that carries charge. Charge is spent on a
    /// placement decision, and a placement decision is made while looking at the machine across
    /// a room — so the machine has to answer "would a hold be refused" on its own body, without
    /// the player looking away at the HUD. That is what the crystal is for.</para>
    /// <para>Split out of the main file so both halves stay under this tree's file cap; the
    /// firing clock, targeting and damage intake are next door.</para></summary>
    public sealed partial class TurretObject
    {
        // Spawn stands the root half a metre off the floor, so the plinth hangs that far below
        // the root to meet it. Everything else is measured off the pedestal rather than authored
        // twice, so changing the plinth's height in PylonMesh moves the crystal with it instead
        // of quietly burying it.
        const float RootLift = 0.5f;
        static readonly float PlinthTopLocal = PylonMesh.PlinthRise - RootLift;
        static readonly float RestHeight = PlinthTopLocal + PylonMesh.CrystalDrop;

        /// <summary>Clear air between the plinth's top face and the crystal's lower point. The
        /// gap is the read: a crystal touching the plinth means the machine is spent, so the
        /// floating state has to be unmistakably off the pedestal at a glance.</summary>
        const float FloatGap = 0.25f;
        static readonly float FloatHeight = RestHeight + FloatGap;

        // Authored by eye, not measured — spin and bob are judged on screen. Deployed idles
        // slowly enough to read as waiting; Special is a hard step up in both rate and travel so
        // the overcharge window is tellable from across the room without reading colour.
        const float SpinDegreesPerSecond = 40f;
        const float SpecialSpinDegreesPerSecond = 300f;
        const float BobRadiansPerSecond = 1.7f;
        const float SpecialBobRadiansPerSecond = 5.2f;
        const float BobAmplitudeMetres = 0.055f;
        const float SpecialBobAmplitudeMetres = 0.13f;

        // A tilted spin axis. An octahedron turned about dead-centre Y is four-fold symmetric and
        // the spin barely reads; leaning it makes the crystal tumble instead of merely turning.
        const float TiltDegrees = 14f;

        // How fast the crystal sinks onto the plinth when the machine is spent, and how fast the
        // bob and spin wind down with it. Slow enough to read as a death rather than a teleport.
        const float SettlePerSecond = 2.2f;

        /// <summary>Emission multiplier at full charge. Above 1 so a charged crystal blooms
        /// rather than merely reaching its own albedo.</summary>
        const float EmissionPeak = 2.4f;

        static readonly Color DeployedTone = new Color(0.35f, 0.75f, 1f, 1f);
        static readonly Color SpentTone = new Color(0.24f, 0.25f, 0.28f, 1f);
        /// <summary>What the crystal is made of before any charge lights it: cold, dark stone.
        /// A machine with nothing to spend must not read as merely dimmer blue, or the refusal
        /// is still a refusal the player cannot predict.</summary>
        static readonly Color InertTone = new Color(0.15f, 0.18f, 0.23f, 1f);
        static readonly Color PlinthTone = new Color(0.11f, 0.12f, 0.14f, 1f);

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        // One material per part for every pylon in the room. The previous body built a fresh
        // Material per turret and repainted it in place, so every despawned machine left one
        // behind; the phase colours are a fixed set of three, and per-machine glow rides a
        // property block instead.
        static Material _plinthMaterial;
        static Material _crystalMaterial;

        Transform _plinth;
        Transform _crystalPivot;
        Renderer _crystalRenderer;
        MaterialPropertyBlock _crystalBlock;
        float _spinDegrees;
        float _bobPhase;
        // 1 while the machine is live and floating, 0 once it is spent and resting. One scalar
        // drives the drop, the bob damping and the spin-down together, so the three cannot
        // disagree about whether the machine has died.
        float _settle = 1f;
        Color _shownAlbedo = Color.clear;
        Color _shownEmission = Color.clear;

        /// <summary>The transform that carries aim. Falls back to the root if the plinth failed
        /// to build, so a missing shader costs the look and not the gun.</summary>
        Transform Facing => _plinth != null ? _plinth : transform;

        /// <summary>Stand the pylon under this turret's root. Called once per spawn — both
        /// meshes and both materials are shared statics, so a placement costs two GameObjects
        /// and one property block, not two meshes and two materials.</summary>
        void BuildPylon()
        {
            var root = transform;
            _plinth = MakePart(root, "PylonPlinth", PylonMesh.Plinth, PlinthMaterial);
            _plinth.localPosition = new Vector3(0f, -RootLift, 0f);

            // Parented to the root rather than to the plinth, deliberately: the plinth swings to
            // face the target and the crystal's spin must stay its own, carrying charge instead
            // of encoding where the gun is pointed.
            _crystalPivot = MakePart(root, "PylonCrystal", PylonMesh.Crystal, CrystalMaterial);
            _crystalPivot.localPosition = new Vector3(0f, FloatHeight, 0f);
            _crystalRenderer = _crystalPivot.GetComponent<Renderer>();
            _crystalBlock = new MaterialPropertyBlock();
        }

        static Transform MakePart(Transform host, string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(host, false);
            // Ignore Raycast, as the old primitive body was: the machine is scenery to every
            // aim ray in the game and picking it would put a wall in front of the player's gun.
            int ignore = LayerMask.NameToLayer("Ignore Raycast");
            if (ignore >= 0) go.layer = ignore;
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            if (material != null) renderer.sharedMaterial = material;
            return go.transform;
        }

        /// <summary>The crystal drive: colour by phase, glow by charge, spin and bob by both.
        /// <para>Runs ahead of the presentation-only guard in Update, so a client's scenery copy
        /// shows the same crystal as the server's gun without owning it — the replicated charge
        /// and phase are all this reads.</para>
        /// <para>Called every frame on every live turret in the room, so nothing here allocates:
        /// the property block, the renderer and both transforms were resolved at spawn.</para>
        /// </summary>
        void PushTint()
        {
            var phase = _state.Phase;
            float max = _state.ChargeMax;
            float charge = max > 0f ? _state.Charge / max : 0f;
            if (charge < 0f) charge = 0f;
            if (charge > 1f) charge = 1f;
            PushCrystalLook(phase, charge);
            PushCrystalMotion(phase);
        }

        /// <summary>Albedo and emission. Glow rises continuously with charge rather than
        /// stepping at a threshold, because the question the player asks the machine is not
        /// "is it ready" but "how close", and a step answers only the first.</summary>
        void PushCrystalLook(TurretPhase phase, float charge)
        {
            if (_crystalRenderer == null || _crystalBlock == null) return;

            Color tone = DeployedTone;
            float lit = charge;
            if (phase == TurretPhase.Special)
            {
                tone = FxColors.FxOvercharge;
            }
            else if (phase == TurretPhase.Exhausted)
            {
                // Spent is spent: dark and unlit whatever the charge field happens to hold,
                // so a machine that ended with a sliver left does not read as still usable.
                tone = SpentTone;
                lit = 0f;
            }

            Color albedo = Color.Lerp(InertTone, tone, lit);
            Color emission = tone * (lit * EmissionPeak);
            emission.a = 1f;
            if (albedo == _shownAlbedo && emission == _shownEmission) return;
            _shownAlbedo = albedo;
            _shownEmission = emission;

            // Both colour names are set unconditionally. URP/Lit answers to _BaseColor and the
            // Unlit fallback to _Color; a property block simply drops a name the shader does not
            // expose, so one push covers either without asking the material which it is.
            _crystalBlock.SetColor(BaseColorId, albedo);
            _crystalBlock.SetColor(ColorId, albedo);
            _crystalBlock.SetColor(EmissionColorId, emission);
            _crystalRenderer.SetPropertyBlock(_crystalBlock);
        }

        /// <summary>Spin, bob and rest height. Exhausted lowers the crystal onto the plinth and
        /// stops it, which is the one phase read that survives being seen in silhouette.</summary>
        void PushCrystalMotion(TurretPhase phase)
        {
            if (_crystalPivot == null) return;
            float dt = Time.deltaTime;
            bool spent = phase == TurretPhase.Exhausted;
            _settle = Mathf.MoveTowards(_settle, spent ? 0f : 1f, SettlePerSecond * dt);

            bool special = phase == TurretPhase.Special;
            float spinRate = special ? SpecialSpinDegreesPerSecond : SpinDegreesPerSecond;
            float bobRate = special ? SpecialBobRadiansPerSecond : BobRadiansPerSecond;
            float amplitude = special ? SpecialBobAmplitudeMetres : BobAmplitudeMetres;

            // Scaling both rates by the settle scalar is what makes the machine wind down rather
            // than halt: the crystal slows as it sinks and is motionless the moment it lands.
            _spinDegrees += spinRate * _settle * dt;
            if (_spinDegrees >= 360f) _spinDegrees -= 360f;
            _bobPhase += bobRate * _settle * dt;
            if (_bobPhase >= Mathf.PI * 2f) _bobPhase -= Mathf.PI * 2f;

            _crystalPivot.localRotation = Quaternion.Euler(0f, _spinDegrees, TiltDegrees);
            Vector3 local = _crystalPivot.localPosition;
            local.y = Mathf.Lerp(RestHeight, FloatHeight, _settle)
                + Mathf.Sin(_bobPhase) * amplitude * _settle;
            _crystalPivot.localPosition = local;
        }

        static Material PlinthMaterial
        {
            get
            {
                if (_plinthMaterial == null) _plinthMaterial = Shared(PlinthTone, false);
                return _plinthMaterial;
            }
        }

        static Material CrystalMaterial
        {
            get
            {
                if (_crystalMaterial == null) _crystalMaterial = Shared(InertTone, true);
                return _crystalMaterial;
            }
        }

        /// <summary>One shared material per part, built once for the whole room. A missing
        /// shader degrades to an untinted pylon rather than a magenta one, and to a lit-looking
        /// crystal rather than no crystal — the shape still carries the phase read.</summary>
        static Material Shared(Color tone, bool emissive)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) return null;
            var mat = new Material(shader);
            mat.hideFlags = HideFlags.HideAndDontSave;
            mat.color = tone;
            // The keyword has to be on the material: a property block can set _EmissionColor but
            // cannot switch the variant, so without this the crystal never glows at all.
            if (emissive && mat.HasProperty(EmissionColorId))
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            return mat;
        }
    }
}
