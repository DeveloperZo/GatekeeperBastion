using GatekeeperBastion.Util;
using Michsky.UI.Shift;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: boot splash (M35 criteria 1-2).
    /// Same Shift backdrop stack as MainMenu (video + glow motes), cool cyan tint so
    /// Splash reads apart from the menu's warm orange. Title stays OnGUI so TMP/Canvas
    /// wiring cannot blank it. MainMenu is not preloaded.</summary>
    public sealed class SplashController : MonoBehaviour
    {
        const string Tag = "Splash";
        const string Title = "GATEKEEPER BASTION";
        const float FadeInSeconds = 0.4f;
        const float HoldSeconds = 1.6f;
        const float FadeOutSeconds = 0.5f;
        const float InputArmSeconds = 0.35f;
        const float MaxStepSeconds = 1f / 30f;

        // Menu uses warm orange (1, 0.55, 0.1). Splash uses cool cyan.
        static readonly Color BackdropTint = new Color(0.25f, 0.70f, 1.00f, 0.40f);
        static readonly Color ParticleTint = new Color(0.35f, 0.85f, 1.00f, 0.32f);
        static readonly Color ClearColor = new Color(0.02f, 0.04f, 0.08f, 1f);

        [SerializeField] CanvasGroup _fadeGroup;
        [Tooltip("Shift Glow Mote material (same as MainMenu Glow Motes).")]
        [SerializeField] Material _moteMaterial;

        bool _activated;
        bool _failed;
        float _elapsed;
        float _alpha;
        GUIStyle _titleStyle;
        CanvasGroup _backdropGroup;
        RenderTexture _videoRt;

        void Awake()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _alpha = 0f;

            // Authored Splash canvas (black TMP card) is retired — backdrop + OnGUI title replace it.
            if (_fadeGroup == null)
                _fadeGroup = FindFirstObjectByType<CanvasGroup>();
            if (_fadeGroup != null)
                _fadeGroup.gameObject.SetActive(false);

            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = ClearColor;
            }

            BuildBackdrop();
            Log.Event(Tag, "title card — Shift backdrop (cyan tint), any key/click skips after arm");
        }

        void OnDestroy()
        {
            if (_videoRt != null)
            {
                _videoRt.Release();
                Destroy(_videoRt);
                _videoRt = null;
            }
        }

        void BuildBackdrop()
        {
            var root = new GameObject("Splash Backdrop");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1280f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>().enabled = false;

            _backdropGroup = root.AddComponent<CanvasGroup>();
            _backdropGroup.alpha = 0f;
            _backdropGroup.blocksRaycasts = false;
            _backdropGroup.interactable = false;

            // 1) Solid blank (same role as MainMenu Background/Blank).
            MakeStretchImage(root.transform, "Blank", new Color(0f, 0f, 0f, 1f), null);

            var ui = Resources.Load<UIManager>("Shift UI Manager");
            if (ui != null)
            {
                // 2) Advanced video plate — same clip as menu, splash tint.
                if (ui.backgroundVideo != null)
                    MakeVideoPlate(root.transform, ui);
                // 3) Basic image plate as fall-back / extra depth.
                if (ui.backgroundImage != null)
                    MakeStretchImage(root.transform, "Basic Background", BackdropTint, ui.backgroundImage);
            }

            // 4) Glow motes — same material/feel as menu, cyan startColor.
            MakeGlowMotes(root.transform);
        }

        void MakeVideoPlate(Transform parent, UIManager ui)
        {
            var go = new GameObject("Advanced Background", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());

            _videoRt = new RenderTexture(1920, 1080, 0)
            {
                name = "SplashBackdropRT",
                filterMode = FilterMode.Bilinear
            };

            var raw = go.AddComponent<RawImage>();
            raw.texture = _videoRt;
            raw.color = BackdropTint;
            raw.raycastTarget = false;

            var vp = go.AddComponent<VideoPlayer>();
            vp.playOnAwake = true;
            vp.isLooping = true;
            vp.skipOnDrop = true;
            vp.renderMode = VideoRenderMode.RenderTexture;
            vp.targetTexture = _videoRt;
            vp.clip = ui.backgroundVideo;
            vp.playbackSpeed = ui.backgroundSpeed > 0.01f ? ui.backgroundSpeed : 1.25f;
            vp.audioOutputMode = VideoAudioOutputMode.None;
            vp.EnableAudioTrack(0, false);
            vp.Play();
        }

        static Image MakeStretchImage(Transform parent, string name, Color color, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());
            var img = go.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            img.raycastTarget = false;
            img.preserveAspect = false;
            return img;
        }

        void MakeGlowMotes(Transform parent)
        {
            var go = new GameObject("Glow Motes", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            Stretch(rt);
            // MainMenu Glow Motes use scale 200 under the canvas hierarchy.
            rt.localScale = new Vector3(200f, 200f, 200f);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = true;
            main.startLifetime = 5f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.15f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = ParticleTint;
            main.maxParticles = 64;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var emission = ps.emission;
            emission.rateOverTime = 8f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Rectangle;
            shape.scale = new Vector3(8f, 5f, 1f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.2f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = grad;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var moteMat = ResolveMoteMaterial();
            if (moteMat != null)
                renderer.material = moteMat;
            renderer.enabled = false; // UIParticleRenderer owns the draw

            go.AddComponent<CanvasRenderer>();
            var uiParticle = go.AddComponent<UIParticleRenderer>();
            uiParticle.fixedTime = true;
            if (moteMat != null)
                uiParticle.material = moteMat;

            ps.Play(true);
        }

        Material ResolveMoteMaterial()
        {
            if (_moteMaterial != null) return _moteMaterial;
            // Fallback: find Glow Mote if it is already loaded with the Shift package.
            var loaded = Resources.FindObjectsOfTypeAll<Material>();
            for (int i = 0; i < loaded.Length; i++)
            {
                if (loaded[i] != null && loaded[i].name == "Glow Mote")
                    return loaded[i];
            }
            Log.Warn(Tag, "Glow Mote material missing — particles may be pink/default");
            return null;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
        }

        void Update()
        {
            if (_activated) return;

            float dt = Mathf.Min(Time.unscaledDeltaTime, MaxStepSeconds);
            _elapsed += dt;
            _alpha = AlphaAt(_elapsed);
            if (_backdropGroup != null)
                _backdropGroup.alpha = _alpha;

            bool armed = _elapsed >= InputArmSeconds;
            if (armed && ReadAnyInput())
            {
                Log.Event(Tag, "skipped by input");
                Advance();
                return;
            }

            if (_elapsed >= FadeInSeconds + HoldSeconds + FadeOutSeconds)
            {
                Log.Event(Tag, "hold complete — advancing");
                Advance();
            }
        }

        void OnGUI()
        {
            if (_activated) return;
            EnsureStyle();
            Color prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, _alpha);
            float h = Mathf.Max(48f, Screen.height * 0.08f);
            GUI.Label(new Rect(0f, (Screen.height - h) * 0.5f, Screen.width, h), Title, _titleStyle);
            GUI.color = prev;
        }

        void EnsureStyle()
        {
            if (_titleStyle != null) return;
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.055f), 28, 72),
                normal = { textColor = Color.white },
                hover = { textColor = Color.white },
                active = { textColor = Color.white },
            };
        }

        static float AlphaAt(float t)
        {
            if (t < FadeInSeconds) return Mathf.Clamp01(t / FadeInSeconds);
            t -= FadeInSeconds;
            if (t < HoldSeconds) return 1f;
            t -= HoldSeconds;
            return Mathf.Clamp01(1f - t / FadeOutSeconds);
        }

        static bool ReadAnyInput() =>
            Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);

        public void Skip() => Advance();

        void Advance()
        {
            if (_activated) return;
            _activated = true;
            Log.Event(Tag, "advancing to MainMenu");
            if (SceneFlow.LoadMainMenu()) return;
            if (SceneFlow.CanLoad(SceneFlow.MainMenu))
            {
                SceneManager.LoadScene(SceneFlow.MainMenu);
                return;
            }
            if (!_failed)
            {
                _failed = true;
                Log.Error(Tag, "Could not load MainMenu from the splash.");
            }
        }
    }
}
