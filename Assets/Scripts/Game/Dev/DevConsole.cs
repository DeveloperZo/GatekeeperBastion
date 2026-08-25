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
    public sealed partial class DevConsole : MonoBehaviour
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

        enum CaptureKind : byte { None, JumpDigit, LoadoutPicker, CheckpointDigit }

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
                case CaptureKind.CheckpointDigit:
                    // Beats are letters (A..): also mute the fight verbs that live on
                    // letters in that range, or picking beat E would hold-interact and
                    // beat G would fire the crew ability.
                    return id == ControlId.SlotPrimary
                        || id == ControlId.SlotSecondary
                        || id == ControlId.SlotDrone
                        || id == ControlId.Interact
                        || id == ControlId.WeaponAbility
                        || id == ControlId.CrewAbility
                        || id == ControlId.PersonalAbility;
                case CaptureKind.LoadoutPicker:
                    return id == ControlId.SlotPrimary
                        || id == ControlId.SlotSecondary
                        || id == ControlId.SlotDrone
                        || id == ControlId.SlotLast
                        || id == ControlId.Interact;
                default:
                    // One-frame linger after a digit or letter was consumed this Update.
                    return id == ControlId.SlotPrimary
                        || id == ControlId.SlotSecondary
                        || id == ControlId.SlotDrone
                        || id == ControlId.Interact
                        || id == ControlId.WeaponAbility
                        || id == ControlId.CrewAbility
                        || id == ControlId.PersonalAbility;
            }
        }

        bool _overlay;
        GameController _gameCached;
        bool _awaitingWaveDigit;
        bool _awaitingCheckpointDigit;
        bool _awaitingRangeDigit;
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
            else if (_awaitingCheckpointDigit)
            {
                _capture = CaptureKind.CheckpointDigit;
                _suppressFightKeys = true;
            }
            // The range digit borrows the jump digit's capture: same shape, same suppression.
            else if (_awaitingRangeDigit) { _capture = CaptureKind.JumpDigit; _suppressFightKeys = true; }
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
                if (TryReadDigit(WaveDirector.MaxWaves, out int wave))
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

            if (_awaitingCheckpointDigit && TickCheckpointDigit())
                return;

            if (_awaitingRangeDigit && TickRangeDigit()) return;
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
            if (action != DevCommands.Kind.ArmDemoCheckpoint
                && action != DevCommands.Kind.ArmCoopCheckpoint)
                _awaitingCheckpointDigit = false;
            if (action != DevCommands.Kind.ArmJumpToWave) _awaitingRangeDigit = false;

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
                    _overlay = true; // discovery: show that a digit is expected
                    if (TryArmRange()) break;
                    _awaitingWaveDigit = true;
                    Log.Event(Tag, $"jump armed — press 1..{WaveDirector.MaxWaves}");
                    break;

                case DevCommands.Kind.ClearGatekeepers:
                    ClearRangeLane();
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

                case DevCommands.Kind.ArmDemoCheckpoint:
                    ArmCheckpointPrompt(coop: false);
                    break;

                case DevCommands.Kind.ArmCoopCheckpoint:
                    ArmCheckpointPrompt(coop: true);
                    break;

                case DevCommands.Kind.ToggleFaucetLoop:
                    if (gc != null) gc.ToggleFaucetLoop();
                    else Log.Warn(Tag, "ToggleFaucetLoop ignored — no GameController.");
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

        static bool TryReadDigit(int max, out int digit)
        {
            digit = 0;
            for (int d = 1; d <= max && d <= 9; d++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + d) || Input.GetKeyDown(KeyCode.Keypad0 + d))
                {
                    digit = d;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Letter capture (A..): 1-based row index of the pressed letter, so beat
        /// rows never collide with the weapon-slot digits. Capped at Z.</summary>
        static bool TryReadLetter(int max, out int index)
        {
            index = 0;
            for (int i = 1; i <= max && i <= 26; i++)
            {
                if (Input.GetKeyDown(KeyCode.A + (i - 1)))
                {
                    index = i;
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

        /// <summary>Two panels, never both: the top-level keys, or the digits of the one key
        /// that is armed.
        ///
        /// An armed menu REPLACES the binding list rather than stacking under it. While a menu
        /// is up a digit is the only thing that does anything, so the twelve parent rows are
        /// twelve lines the eye has to skip past to reach the four that can be pressed — and
        /// the range's nine rows used to be up permanently, whether or not F3 had been touched.
        /// Esc comes back. Rows that open a menu are marked, so the parent says which keys have
        /// a second level without the player having to press them to find out.</summary>
        void DrawOverlay(float vw)
        {
            float w = Mathf.Min(OverlayWidth, vw - 32f);
            float x = vw - w - 16f;
            float y = OverlayTop;

            if (_awaitingCheckpointDigit) { DrawCheckpointRows(x, y, w); return; }
            if (_awaitingRangeDigit) { DrawRangeRows(x, y, w); return; }
            if (_awaitingWaveDigit)
            {
                // No printed row list: the digits are "any wave", not a named menu.
                GUI.Label(new Rect(x, y, w, 22f),
                    $"F3  JUMP TO WAVE — press 1..{WaveDirector.MaxWaves}, Esc back", _title);
                return;
            }

            GUI.Label(new Rect(x, y, w, 22f), "DEV MODE", _title);
            DrawBindingRows(x, y + 26f, w);
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
            // A range is a tool, and a tool whose controls are hidden behind a key you have to
            // know about is not one. Everywhere else the overlay stays off until F1.
            if (HasRange) _overlay = true;
        }
    }
}
#endif
