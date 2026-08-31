using System.Text;
using GatekeepersProtocol.Sim;
using TMPro;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: the Ship hub's HUD — uGUI via <see cref="ShipHudView"/>.
    /// Bastion corner cards (Profile / Score / Weapon; no Vitals), crosshair, and interact
    /// prompt when looking at a console. No slogans, objectives, or shortcut strip on the
    /// corners — those belong in station panels. Visibility is state-change only;
    /// steady-state LateUpdate writes values only.</summary>
    public sealed class ShipHud : MonoBehaviour
    {
        const float RebindInterval = 0.5f;

        [SerializeField] ShipHudView _view;

        [SerializeField, Tooltip("Ship-hub controller. Authored in the scene.")]
        ShipController _ship;

        [SerializeField, Tooltip("Root under which the billboarded TextMesh station labels live. Authored in the scene.")]
        Transform _labelsRoot;

        [SerializeField, OptionalBinding, Tooltip("Isometric rig (Ship probe). While wired and " +
            "enabled, the OS cursor is the aim point: crosshair hidden, cursor-free warn " +
            "suppressed, interact prompt kept. Null or disabled means first-person behavior, " +
            "unchanged.")]
        IsometricCameraRig _isoRig;

        Interactor _interactor;
        FirstPersonController _fpc;
        Transform _camera;
        Camera _eye;
        TextMesh[] _labels;
        RectTransform _overlayCanvasRect;
        RectTransform[] _overlayRects;
        bool _labelsScanned;
        bool _overlayWarned;
        float _rebindTimer;
        bool _hudRelevant = true;
        long _cachedSalvage = long.MinValue;
        int _cachedLoadoutHash = int.MinValue;
        int _cachedRuns = int.MinValue;
        int _cachedDeepest = int.MinValue;
        int _cachedProtocol = int.MinValue;
        string _cachedPrompt = "\0";
        bool _cachedCursorFree;
        bool _cachedCrosshairOn = true;
        bool _labelArmoryOn = true;
        bool _labelCrewOn = true;
        bool _labelFabOn = true;

        readonly StringBuilder _lineBuf = new StringBuilder(160);

        void Awake()
        {
            if (_ship == null) _ship = GetComponent<ShipController>();
            UiCanvasGuard.RepairIfBroken(gameObject);
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                canvas.transform.localScale = Vector3.one;
        }

        void Start()
        {
            Bind();
            if (_view == null) _view = GetComponent<ShipHudView>();
            if (_view == null) _view = GetComponentInChildren<ShipHudView>(true);
            ApplyRelevance(true);
            if (_view != null)
            {
                _view.EnsureBuilt();
                // Same Profile slots as Bastion until a real level/XP model exists.
                _view.SetProfile(ShipHudView.ProfileRank, null);
                _view.SetProtocolLine("NO PROTOCOL");
                _view.SetScore("", "");
                _view.SetWeapon("--", "--");
                _view.SetCrosshairVisible(true);
            }
        }

        void LateUpdate()
        {
            _rebindTimer -= Time.deltaTime;
            if (_rebindTimer <= 0f && (_interactor == null || _camera == null || _fpc == null))
            {
                _rebindTimer = RebindInterval;
                Bind();
            }
            OptionsScreen.TickDisplayRevert();
            ApplyLabelVisibility(GameSession.Meta);
            if (_overlayRects == null) EnsureOverlay();
            ProjectLabels();
            PushHud();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnGUI()
        {
            float scale = Screen.height / 1080f;
            if (scale < 0.01f) scale = 1f;
            var prev = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            DevTaintBanner.Draw(1920f);
            GUI.matrix = prev;
        }
#endif

        void PushHud()
        {
            if (_view == null) return;
            var m = GameSession.Meta;
            bool hide = m == null || OptionsScreen.IsOpen
                || (_ship != null && (_ship.IsPanelOpen || _ship.IsSettingsOpen));
            ApplyRelevance(!hide);
            if (hide || m == null) return;

            // Salvage and depth belong to the realm being stood in, which on a guest is the
            // host's — not the one in this machine's save. Reading locally put the HUD at odds
            // with the plate on the wall a few metres away.
            var record = RealmRecordSource.Current();
            long banked = RealmRecordSource.Banked();
            int loadoutHash = LoadoutHash(m);
            int deepest = record.DeepestWave;
            int runs = m.TotalRuns;
            int protocol = (int)m.EquippedProtocol;
            if (_cachedSalvage != banked || _cachedLoadoutHash != loadoutHash
                || _cachedRuns != runs || _cachedDeepest != deepest
                || _cachedProtocol != protocol)
            {
                _cachedSalvage = banked;
                _cachedLoadoutHash = loadoutHash;
                _cachedRuns = runs;
                _cachedDeepest = deepest;
                _cachedProtocol = protocol;

                _view.SetProfile(ShipHudView.ProfileRank, null);
                string protoName = BoarderProtocol.ProtocolName(m.EquippedProtocol);
                _view.SetProtocolLine(string.IsNullOrEmpty(protoName) ? "NO PROTOCOL" : protoName);

                _lineBuf.Clear();
                _lineBuf.Append("Salvage ").Append(banked);
                string salvage = _lineBuf.ToString();
                _lineBuf.Clear();
                if (deepest < 1) deepest = 1;
                _lineBuf.Append("cleared ").Append(deepest)
                    .Append("  -  ").Append(runs).Append(" boardings");
                _view.SetScore(salvage, _lineBuf.ToString());

                AppendLoadout(_view, m);
            }

            // A Lock line frees the cursor on purpose — the recapture warn would lie there.
            // Iso mode never locks the cursor, so "free" is its working state: no warn, and
            // the prompt path below must stay live.
            bool iso = _isoRig != null && _isoRig.isActiveAndEnabled;
            bool cursorFree = !iso && Cursor.lockState != CursorLockMode.Locked
                && !DemoVoice.LocksInput;
            if (cursorFree != _cachedCursorFree)
            {
                _cachedCursorFree = cursorFree;
                _view.SetCursorFreeWarn(cursorFree);
                if (cursorFree) _cachedPrompt = "\0";
            }

            bool crosshairOn = !iso && !DemoShipFight.WakeHidesCrosshair;
            if (crosshairOn != _cachedCrosshairOn)
            {
                _cachedCrosshairOn = crosshairOn;
                _view.SetCrosshairVisible(crosshairOn);
            }

            if (!cursorFree && !DemoVoice.LocksInput)
            {
                string prompt = _interactor != null && !string.IsNullOrEmpty(_interactor.CurrentPrompt)
                    ? _interactor.CurrentPrompt
                    : "";
                // Demo idle prompt: with nothing targeted, the standing objective is the line.
                if (prompt.Length == 0)
                {
                    string standing = DemoShipFight.StandingObjective;
                    if (standing != null) prompt = standing;
                }
                if (prompt != _cachedPrompt)
                {
                    _cachedPrompt = prompt;
                    _view.SetPrompt(prompt);
                }
            }
        }

        void ApplyRelevance(bool relevant)
        {
            if (relevant == _hudRelevant) return;
            _hudRelevant = relevant;
            if (_view != null) _view.SetHudRelevant(relevant);
        }

        void Bind()
        {
            // Registry only (the sanctioned lookup); the rebind timer retries until the rig registers.
            var player = PlayerRegistry.Local;
            if (player != null)
            {
                _interactor = player.GetComponentInChildren<Interactor>();
                _fpc = player.GetComponent<FirstPersonController>();
            }

            _camera = _fpc != null ? _fpc.cameraTransform : null;
            _eye = ResolveEye();

            if (!_labelsScanned)
            {
                _labelsScanned = true;
                _labels = _labelsRoot != null
                    ? _labelsRoot.GetComponentsInChildren<TextMesh>(true)
                    : null;
            }
            if (_overlayRects == null) EnsureOverlay();
        }

        Camera ResolveEye()
        {
            if (_camera != null)
            {
                var cam = _camera.GetComponent<Camera>();
                if (cam != null) return cam;
            }
            if (_isoRig != null && _isoRig.isActiveAndEnabled)
            {
                var cam = _isoRig.GetComponentInChildren<Camera>(true);
                if (cam != null) return cam;
            }
            return Camera.main;
        }

        /// <summary>Hidden stations show no name (2026-08-18 revision): the world label
        /// stays off until the station has a live function. NAV ("MAP") is always
        /// visible; demo-off verdicts are all Open, so authored labels never change.
        /// State-change only — the caches start true to match the authored scene.</summary>
        void ApplyLabelVisibility(MetaState m)
        {
            if (_labels == null) return;
            bool armory = DemoProgress.ArmoryVerdict(m) != DemoStationVerdict.Hidden;
            bool crew = DemoProgress.CrewVerdict(m) != DemoStationVerdict.Hidden;
            bool fab = DemoProgress.FabricationVerdict(m) != DemoStationVerdict.Hidden;
            if (armory == _labelArmoryOn && crew == _labelCrewOn && fab == _labelFabOn)
                return;
            _labelArmoryOn = armory;
            _labelCrewOn = crew;
            _labelFabOn = fab;
            for (int i = 0; i < _labels.Length; i++)
            {
                var label = _labels[i];
                if (label == null) continue;
                bool on;
                if (label.text == "ARMORY") on = armory;
                else if (label.text == "CREW QUARTERS") on = crew;
                else if (label.text == "FABRICATION") on = fab;
                else continue; // MAP and anything unnamed stay authored
                if (label.gameObject.activeSelf != on)
                    label.gameObject.SetActive(on);
            }
        }

        /// <summary>World TextMesh stays the authored source (text, colour, world point).
        /// Glyphs are Overlay copies so they composite after <see cref="PixelRender"/>,
        /// the same path as the HUD. Building those copies here is the view of that
        /// source, not a scene rebuild.</summary>
        void EnsureOverlay()
        {
            if (_labels == null || _labels.Length == 0) return;
            Canvas canvas = null;
            if (_view != null) canvas = _view.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                if (!_overlayWarned)
                {
                    _overlayWarned = true;
                    Debug.LogError("[ShipHud] Station labels need a Screen Space Overlay canvas " +
                        "so PixelRender cannot crush them. World TextMesh left as-is.", this);
                }
                return;
            }

            Transform parent = canvas.transform;
            Transform existing = parent.Find("Station Labels");
            RectTransform root;
            if (existing != null) root = existing.GetComponent<RectTransform>();
            else
            {
                var go = new GameObject("Station Labels", typeof(RectTransform));
                go.layer = canvas.gameObject.layer;
                root = go.GetComponent<RectTransform>();
                root.SetParent(parent, false);
                root.anchorMin = Vector2.zero;
                root.anchorMax = Vector2.one;
                root.offsetMin = Vector2.zero;
                root.offsetMax = Vector2.zero;
            }
            _overlayCanvasRect = root;

            TMP_FontAsset font = null;
            var sample = canvas.GetComponentInChildren<TextMeshProUGUI>(true);
            if (sample != null) font = sample.font;

            _overlayRects = new RectTransform[_labels.Length];
            for (int i = 0; i < _labels.Length; i++)
            {
                var mesh = _labels[i];
                if (mesh == null) continue;
                var rend = mesh.GetComponent<MeshRenderer>();
                if (rend != null) rend.enabled = false;

                var child = new GameObject(mesh.text, typeof(RectTransform), typeof(TextMeshProUGUI));
                child.layer = canvas.gameObject.layer;
                var rt = child.GetComponent<RectTransform>();
                rt.SetParent(root, false);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(480f, 48f);
                var tmp = child.GetComponent<TextMeshProUGUI>();
                tmp.text = mesh.text;
                tmp.color = mesh.color;
                tmp.fontSize = 28f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;
                tmp.enableWordWrapping = false;
                tmp.overflowMode = TextOverflowModes.Overflow;
                if (font != null) tmp.font = font;
                _overlayRects[i] = rt;
            }
        }

        void ProjectLabels()
        {
            if (_labels == null || _overlayRects == null || _eye == null
                || _overlayCanvasRect == null) return;
            for (int i = 0; i < _labels.Length; i++)
            {
                var mesh = _labels[i];
                var overlay = _overlayRects[i];
                if (mesh == null || overlay == null) continue;
                Vector3 sp = _eye.WorldToScreenPoint(mesh.transform.position);
                bool show = mesh.gameObject.activeInHierarchy && sp.z > 0f;
                if (overlay.gameObject.activeSelf != show)
                    overlay.gameObject.SetActive(show);
                if (!show) continue;
                Vector2 local;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _overlayCanvasRect, sp, null, out local))
                    continue;
                overlay.anchoredPosition = local;
            }
        }

        static void AppendLoadout(ShipHudView view, MetaState m)
        {
            if (!m.OwnsAnyWeapon)
            {
                view.SetWeapon("--", "--");
                return;
            }
            string primary = m.EquippedPrimary != WeaponId.None
                ? WeaponCatalog.Def(m.EquippedPrimary).displayName
                : "--";
            string secondary = m.EquippedSecondary != WeaponId.None
                ? WeaponCatalog.Def(m.EquippedSecondary).displayName
                : "--";
            view.SetWeapon(primary, secondary);
        }

        static int LoadoutHash(MetaState m) =>
            ((int)m.EquippedPrimary * 397) ^ (int)m.EquippedSecondary;
    }
}
