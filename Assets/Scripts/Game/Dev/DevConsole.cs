#if UNITY_EDITOR || DEVELOPMENT_BUILD
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: editor/dev-build console. F1 toggles an absolute-GUI overlay of every
    /// binding (top-right — clear of the top-left run HUD); each action calls a
    /// <see cref="GameController"/> command (never RunSim / Meta / ShipCommands directly).
    /// Entire file compiles out of release player builds.</summary>
    /// <remarks>
    /// No GUILayout: same Bastion HUD gotcha — a layout-stack fault during a fight would hide the
    /// only exit. Styles are per-instance. F7's loadout picker applies via GameController.SetLoadout.
    /// </remarks>
    public sealed class DevConsole : MonoBehaviour
    {
        const string Tag = "Dev";
        const float VirtualHeight = 900f;
        const long GrantAmount = 5000L;

        /// <summary>HudController fight panel is top-left ~416 wide. Dev chrome stays off that
        /// band and clear of the bottom-left shortcut panel; F1 lives top-right.</summary>
        const float OverlayWidth = 420f;
        const float OverlayTop = 16f;

        /// <summary>True while Dev owns keys that would otherwise fire fight bindings
        /// (F3 jump digit, F7 loadout picker). Cleared in LateUpdate once capture ends.</summary>
        static bool _suppressFightKeys;
        static CaptureKind _capture;

        enum CaptureKind : byte { None, JumpDigit, LoadoutPicker }

        /// <summary>Whether Dev is currently eating a fight binding so PlayerGun / Interact
        /// do not double-fire on the same keypress.</summary>
        public static bool Suppresses(ControlId id)
        {
            if (!_suppressFightKeys) return false;
            switch (_capture)
            {
                case CaptureKind.JumpDigit:
                    return id == ControlId.SlotPrimary
                        || id == ControlId.SlotSecondary
                        || id == ControlId.SlotDrone;
                case CaptureKind.LoadoutPicker:
                    return id == ControlId.SlotPrimary
                        || id == ControlId.SlotSecondary
                        || id == ControlId.SlotDrone
                        || id == ControlId.SlotLast
                        || id == ControlId.Interact;
                default:
                    // One-frame linger after a digit was consumed this Update.
                    return id == ControlId.SlotPrimary
                        || id == ControlId.SlotSecondary
                        || id == ControlId.SlotDrone;
            }
        }

        bool _overlay;
        GameController _gameCached;
        bool _awaitingWaveDigit;
        bool _gatekeeperReadout;
        bool _loadoutPicker;
        bool _guiFaulted;

        // F7 picker: 1/2 pick Primary/Secondary, Q/E cycle shop weapons in that slot, Z/C tier.
        WeaponSlot _pickerSlot = WeaponSlot.Primary;
        int _loadoutWeapon;
        int _loadoutTier;

        GUIStyle _label;
        GUIStyle _title;
        GUIStyle _dim;

        void OnDisable()
        {
            _suppressFightKeys = false;
            _capture = CaptureKind.None;
        }

        void LateUpdate()
        {
            RefreshCapture();
        }

        /// <summary>Dev-only locator: cached scene lookup, re-resolved after a scene change
        /// (the cached handle goes fake-null). Null on the Ship scene, by design.</summary>
        GameController Game
        {
            get
            {
                if (_gameCached == null) _gameCached = FindFirstObjectByType<GameController>();
                return _gameCached;
            }
        }

        void RefreshCapture()
        {
            if (_awaitingWaveDigit)
            {
                _capture = CaptureKind.JumpDigit;
                _suppressFightKeys = true;
            }
            else if (_loadoutPicker)
            {
                _capture = CaptureKind.LoadoutPicker;
                _suppressFightKeys = true;
            }
            else
            {
                _capture = CaptureKind.None;
                _suppressFightKeys = false;
            }
        }

        void Update()
        {
            if (_awaitingWaveDigit)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    _awaitingWaveDigit = false;
                    RefreshCapture();
                    Log.Event(Tag, "jump cancelled");
                    return;
                }
                if (TryReadWaveDigit(out int wave))
                {
                    // Keep suppress through the rest of this frame so PlayerGun does not also
                    // treat the digit as a weapon-slot switch.
                    _awaitingWaveDigit = false;
                    _capture = CaptureKind.None;
                    _suppressFightKeys = true;
                    var gc = Game;
                    if (gc != null) gc.JumpToWave(wave);
                    else Log.Warn(Tag, $"JumpToWave {wave} ignored — no GameController (Ship scene?).");
                    return;
                }
            }

            RefreshCapture();

            for (int i = 0; i < DevCommands.All.Length; i++)
            {
                var b = DevCommands.All[i];
#if !UNITY_EDITOR
                if (ShortcutPanel.IsEditorOnlyDevKey(b.Key)) continue;
#endif
                if (!Input.GetKeyDown(b.Key)) continue;
                Invoke(b.Action);
                RefreshCapture();
                return; // one binding per frame
            }

            if (_loadoutPicker) TickLoadoutPickerKeys();
        }

        void Invoke(DevCommands.Kind action)
        {
            if (action != DevCommands.Kind.ArmJumpToWave)
                _awaitingWaveDigit = false;

            var gc = Game;

            switch (action)
            {
                case DevCommands.Kind.ToggleOverlay:
                    _overlay = !_overlay;
                    _awaitingWaveDigit = false;
                    Log.Event(Tag, _overlay ? "overlay open" : "overlay closed");
                    break;

                case DevCommands.Kind.ReplayWave:
                    if (gc != null) gc.ReplayWave();
                    else Log.Warn(Tag, "ReplayWave ignored — no GameController.");
                    break;

                case DevCommands.Kind.ArmJumpToWave:
                    _awaitingWaveDigit = true;
                    _overlay = true; // discovery: show that a digit is expected
                    Log.Event(Tag, $"jump armed — press 1..{WaveDirector.MaxWaves}");
                    break;

                case DevCommands.Kind.ClearGatekeepers:
                    if (gc != null) gc.ClearGatekeepers();
                    else Log.Warn(Tag, "ClearGatekeepers ignored — no GameController.");
                    break;

                case DevCommands.Kind.RestoreHatch:
                    if (gc != null) gc.RestoreHatch();
                    else Log.Warn(Tag, "RestoreHatch ignored — no GameController.");
                    break;

                case DevCommands.Kind.ToggleInvulnerability:
                    if (gc != null) gc.TogglePlayerInvulnerability();
                    else Log.Warn(Tag, "ToggleInvulnerability ignored — no GameController.");
                    break;

                case DevCommands.Kind.LoadoutPicker:
                    _loadoutPicker = !_loadoutPicker;
                    if (_loadoutPicker) _overlay = true;
                    Log.Event(Tag, _loadoutPicker
                        ? "loadout picker open"
                        : "loadout picker closed");
                    break;

                case DevCommands.Kind.GrantSalvage:
                    // Meta-only: works on Ship (no GameController) and Bastion alike.
                    GameController.GrantSalvage(GrantAmount);
                    break;

                case DevCommands.Kind.ToggleGatekeeperReadout:
                    _gatekeeperReadout = !_gatekeeperReadout;
                    // Read-only overlay: log for criterion 5, but do not suppress saves.
                    Log.Event(Tag, _gatekeeperReadout ? "gatekeeper readout on" : "gatekeeper readout off");
                    break;

                case DevCommands.Kind.StartCoopHost:
                    if (!DemoProgress.MultiplayerUnlocked(GameSession.Meta))
                    {
                        Log.Event("Demo", "multiplayer locked — buy an upgrade first");
                        break;
                    }
                    if (gc != null) gc.StartCoopHost();
                    else Log.Warn(Tag, "StartCoopHost refused — no GameController.");
                    break;

                case DevCommands.Kind.StartCoopClient:
                    if (!DemoProgress.MultiplayerUnlocked(GameSession.Meta))
                    {
                        Log.Event("Demo", "multiplayer locked — buy an upgrade first");
                        break;
                    }
                    if (gc != null) gc.StartCoopClient();
                    else Log.Warn(Tag, "StartCoopClient refused — no GameController.");
                    break;
            }
        }

        void TickLoadoutPickerKeys()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _loadoutPicker = false;
                RefreshCapture();
                Log.Event(Tag, "loadout picker closed");
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                _pickerSlot = WeaponSlot.Primary;
                _loadoutWeapon = 0;
            }
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                _pickerSlot = WeaponSlot.Secondary;
                _loadoutWeapon = 0;
            }

            var weapons = SlotWeapons(_pickerSlot);
            if (weapons.Length == 0) return;

            if (Input.GetKeyDown(KeyCode.Q))
                _loadoutWeapon = Mathf.Max(0, _loadoutWeapon - 1);
            if (Input.GetKeyDown(KeyCode.E))
                _loadoutWeapon = Mathf.Min(weapons.Length - 1, _loadoutWeapon + 1);
            if (Input.GetKeyDown(KeyCode.Z)) _loadoutTier = Mathf.Max(0, _loadoutTier - 1);
            if (Input.GetKeyDown(KeyCode.C)) _loadoutTier = Mathf.Min(WeaponTier.MaxTier, _loadoutTier + 1);
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                int i = Mathf.Clamp(_loadoutWeapon, 0, weapons.Length - 1);
                // Keep suppress through this frame so slot/Q/E do not also fire.
                _loadoutPicker = false;
                _capture = CaptureKind.None;
                _suppressFightKeys = true;
                GameController.SetLoadout((int)_pickerSlot, (int)weapons[i], _loadoutTier);
            }
        }

        static WeaponId[] SlotWeapons(WeaponSlot slot)
        {
            var list = new System.Collections.Generic.List<WeaponId>(4);
            foreach (var id in WeaponCatalog.ForSlot(slot))
                list.Add(id);
            return list.ToArray();
        }

        static bool TryReadWaveDigit(out int wave)
        {
            wave = 0;
            for (int d = 1; d <= WaveDirector.MaxWaves; d++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + d) || Input.GetKeyDown(KeyCode.Keypad0 + d))
                {
                    wave = d;
                    return true;
                }
            }
            return false;
        }

        void OnGUI()
        {
            if (!_overlay && !_gatekeeperReadout) return;
            try
            {
                Draw();
            }
            catch (System.Exception e)
            {
                if (!_guiFaulted)
                {
                    _guiFaulted = true;
                    Log.Error(Tag, $"OnGUI faulted once ({e.GetType().Name}: {e.Message}); overlay suppressed.");
                }
            }
        }

        void Draw()
        {
            EnsureStyles();
            float scale = Screen.height / VirtualHeight;
            Matrix4x4 prev = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            float vw = Screen.width / scale;

            if (_overlay) DrawOverlay(vw);
            if (_gatekeeperReadout) DrawGatekeeperReadout(vw);
            if (_loadoutPicker) DrawLoadoutPicker(vw);

            GUI.matrix = prev;
        }

        void DrawOverlay(float vw)
        {
            float w = Mathf.Min(OverlayWidth, vw - 32f);
            float x = vw - w - 16f;
            float y = OverlayTop;

            GUI.Label(new Rect(x, y, w, 22f), _awaitingWaveDigit
                ? $"DEV — press 1..{WaveDirector.MaxWaves} for jump (Esc cancel)"
                : "DEV MODE", _title);
            y += 26f;

            for (int i = 0; i < DevCommands.All.Length; i++)
            {
                var b = DevCommands.All[i];
                GUI.Label(new Rect(x, y, w, 18f), $"{b.Key}  {b.Label}", _label);
                y += 18f;
            }
        }

        void DrawLoadoutPicker(float vw)
        {
            // Under the F1 binding list (title + 9 rows ≈ 200px).
            float w = Mathf.Min(OverlayWidth, vw - 32f);
            float x = vw - w - 16f;
            float y = OverlayTop + 200f;
            var weapons = SlotWeapons(_pickerSlot);
            GUI.Label(new Rect(x, y, w, 20f), "LOADOUT", _title);
            y += 22f;
            GUI.Label(new Rect(x, y, w, 18f),
                $"slot {_pickerSlot}  (1 Primary / 2 Secondary)", _label);
            y += 18f;
            if (weapons.Length == 0)
            {
                GUI.Label(new Rect(x, y, w, 18f), "(no shop weapons in this slot)", _dim);
                return;
            }
            int i = Mathf.Clamp(_loadoutWeapon, 0, weapons.Length - 1);
            var def = WeaponCatalog.Def(weapons[i]);
            GUI.Label(new Rect(x, y, w, 18f),
                $"{def.displayName} T{_loadoutTier}  (Q/E weapon, Z/C tier 0..{WeaponTier.MaxTier})", _label);
            y += 18f;
            GUI.Label(new Rect(x, y, w, 18f), "Enter = apply · Esc = cancel", _dim);
        }

        void DrawGatekeeperReadout(float vw)
        {
            float w = 340f;
            float x = vw - w - 16f;
            if (x < 16f) x = 16f;
            // Drop below the F1 list when the overlay is open so the two do not paint over each other.
            float y = _overlay ? OverlayTop + 200f + (_loadoutPicker ? 100f : 0f) : 120f;

            GUI.Label(new Rect(x, y, w, 20f), "GATEKEEPERS (live)", _title);
            y += 22f;

            var gatekeepers = FindObjectsByType<EnemyGatekeeper>(FindObjectsSortMode.None);
            if (gatekeepers == null || gatekeepers.Length == 0)
            {
                GUI.Label(new Rect(x, y, w, 18f), "(none alive)", _dim);
                return;
            }

            var hatch = HatchStation.Instance;
            for (int i = 0; i < gatekeepers.Length; i++)
            {
                var b = gatekeepers[i];
                if (b == null) continue;
                float dist = hatch != null
                    ? PlanarDist(b.transform.position, hatch.ClosestSurfacePoint(b.transform.position))
                    : -1f;
                string line = $"{b.Kind} → {b.DevTargetLabel}  hatch {dist:0.0}m  parked {b.DevTimeParked:0.0}s";
                GUI.Label(new Rect(x, y, w, 16f), line, _label);
                y += 16f;
                if (y > VirtualHeight - 40f) break;
            }
        }

        static float PlanarDist(Vector3 a, Vector3 b)
        {
            Vector3 d = a - b;
            d.y = 0f;
            return d.magnitude;
        }

        void EnsureStyles()
        {
            if (_label != null) return;
            _label = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.85f, 0.95f, 0.75f, 1f) },
            };
            _title = new GUIStyle(_label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 1f, 0.55f, 1f) },
            };
            _dim = new GUIStyle(_label)
            {
                normal = { textColor = new Color(0.65f, 0.7f, 0.55f, 1f) },
            };
        }

        void OnEnable()
        {
            _label = _title = _dim = null;
            _guiFaulted = false;
        }
    }
}
#endif
