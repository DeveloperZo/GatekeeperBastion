using GatekeeperBastion.Game.Ui;
using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using Michsky.UI.Shift;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: Ship-hub scene controller — guarantees a loaded GameSession
    /// (direct-play fallback), owns cursor/movement while station panels are open, and is the
    /// sole Esc dispatcher for the Ship (Escape contract).</summary>
    /// <remarks>Cursor lock is load-bearing: Interactor refuses to raycast unless
    /// Cursor.lockState == Locked, so a missed lock means nothing in the ship is usable.
    /// SetWalkMode therefore always sets the cursor itself (even with no player rig found)
    /// and re-asserts once if the rig registers after Start.</remarks>
    public sealed class ShipController : MonoBehaviour
    {
        const string Tag = "ShipController";
        const float BindWindowSeconds = 2f;
        const float InteractRange = 3.2f;

        [SerializeField] GameObject _stationCanvas;
        [SerializeField] MainPanelManager _panels;
        [SerializeField] ModalWindowManager _exitModal;
        [SerializeField] GameObject _pauseMenuPanel;
        [SerializeField] GameObject _settingsPanel;

        /// <summary>The station panel currently open, or null while walking.</summary>
        public StationTerminal OpenStation { get; private set; }

        /// <summary>True while any station panel is up (read by ShipHud).</summary>
        public bool IsPanelOpen => OpenStation != null;

        /// <summary>True while the Settings panel is up via Esc / OptionsScreen.</summary>
        public bool IsSettingsOpen { get; private set; }

        /// <summary>QUIT TO MENU on the Ship Settings panel — state is
        /// already saved at purchase time; no confirm.</summary>
        public void QuitToMenu() => SceneFlow.LoadMainMenu();

        FirstPersonController _fpc;
        bool _walking = true;
        bool _warnedNoRig;
        float _bindWindow = BindWindowSeconds;

        void Awake()
        {
            SceneFlow.EnsureEventSystem();
            GameSession.EnsureLoaded();
            if (_stationCanvas != null && _stationCanvas.activeSelf)
                _stationCanvas.SetActive(false);
        }

        void Start()
        {
            SetWalkMode(true);
            if (GameSession.Meta != null)
                GameSession.Meta.PayloadLoadoutLocked = false;
            Log.Event("Ship", "aboard the ship — " + GameSession.Meta.BankedSalvage + " banked, " +
                                        GameSession.Meta.PodEjections + " run(s) lost, " +
                                        GameSession.Meta.CleanExits + " cleared");
        }

        void Update()
        {
            if (Controls.Down(ControlId.FreeCursor))
                HandleEscape();

            // Same open path as Armory-from-anywhere: Interact Down → OpenStationPanel.
            // Target is the console under the camera ray, not a hard-coded Armory.
            if (Controls.Down(ControlId.Interact))
                TryOpenStationFromInteract(ResolveTargetedStation());

            if (_fpc != null || _bindWindow <= 0f) return;
            _bindWindow -= Time.deltaTime;
            var fpc = FindLocalController();
            if (fpc == null) return;
            _fpc = fpc;
            ApplyWalkMode();
        }

        /// <summary>Open the given station panel. Returns false when a panel/Settings is already
        /// up or <paramref name="station"/> is null. Call site for Interact Down on Ship.</summary>
        public bool TryOpenStationFromInteract(StationTerminal station)
        {
            if (station == null || OpenStation != null || IsSettingsOpen || OptionsScreen.IsOpen)
                return false;
            OpenStationPanel(station);
            return true;
        }

        /// <summary>Camera-forward ray, same range as <see cref="Interactor"/> — self-contained
        /// so open does not depend on Interactor script order.</summary>
        static StationTerminal ResolveTargetedStation()
        {
            var cam = Camera.main;
            if (cam == null) return null;
            if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit,
                    InteractRange))
                return null;
            return hit.collider.GetComponentInParent<StationTerminal>();
        }

        void LateUpdate()
        {
            // Esc is FreeCursor on Ship; FirstPersonController also unlocks on that binding.
            // After the Escape contract, walking means locked — re-assert once per frame so FPC
            // cannot leave the hub cursor-free after Esc opened/closed Settings or a station.
            if (_walking && OpenStation == null && !IsSettingsOpen && !OptionsScreen.IsOpen)
                ApplyWalkMode();
        }

        /// <summary>Ship Esc priority: open modal → close; station open → Close; Options tabs open
        /// → back to Pause Menu; Pause Menu open → close; otherwise → open Pause Menu (Escape
        /// contract).</summary>
        void HandleEscape()
        {
            if (_exitModal != null && _exitModal.gameObject.activeInHierarchy)
            {
                _exitModal.ModalWindowOut();
                return;
            }

            if (OpenStation != null)
            {
                OpenStation.Close();
                return;
            }

            if (OptionsScreen.IsOpen)
            {
                BackToPauseMenu();
                return;
            }

            if (IsSettingsOpen)
            {
                CloseSettings();
                return;
            }

            OpenSettings();
        }

        void OpenSettings()
        {
            if (_stationCanvas != null && !_stationCanvas.activeSelf) _stationCanvas.SetActive(true);
            if (_settingsPanel != null && _settingsPanel.activeSelf) _settingsPanel.SetActive(false);
            if (_pauseMenuPanel != null) _pauseMenuPanel.SetActive(true);
            IsSettingsOpen = true;
            SetWalkMode(false);
        }

        /// <summary>Settings button on the Pause Menu shell — reveals the Options tabs screen.</summary>
        public void OpenOptionsFromPauseMenu()
        {
            if (_pauseMenuPanel != null) _pauseMenuPanel.SetActive(false);
            if (_settingsPanel != null) _settingsPanel.SetActive(true);
            OptionsScreen.Open(_fpc != null ? _fpc : FindLocalController());
        }

        /// <summary>Resume button on the Pause Menu shell — same as pressing Esc from the shell.</summary>
        public void ResumeFromPauseMenu() => CloseSettings();

        void CloseSettings()
        {
            if (OptionsScreen.IsOpen) OptionsScreen.Close();
            if (_settingsPanel != null) _settingsPanel.SetActive(false);
            if (_pauseMenuPanel != null) _pauseMenuPanel.SetActive(false);
            IsSettingsOpen = false;
            if (_stationCanvas != null && _stationCanvas.activeSelf && OpenStation == null)
                _stationCanvas.SetActive(false);
            SetWalkMode(true);
        }

        /// <summary>Esc from the Options tabs, or its own back button's runtime path, steps back to
        /// the Pause Menu shell rather than fully closing (QuitToMenu/X-close still fully close).</summary>
        void BackToPauseMenu()
        {
            OptionsScreen.Close();
            if (_settingsPanel != null) _settingsPanel.SetActive(false);
            if (_pauseMenuPanel != null) _pauseMenuPanel.SetActive(true);
        }

        /// <summary>Open a station panel: canvas on, cursor freed, movement locked. One at a time.</summary>
        public void OpenStationPanel(StationTerminal station)
        {
            if (station == null || OpenStation == station) return;
            if (IsSettingsOpen) CloseSettings();
            OpenStation = station;
            SetWalkMode(false);
            station.OnOpenedByShip();
        }

        /// <summary>Close the open panel (if it is the given one) and return to walking.</summary>
        public void CloseStationPanel(StationTerminal station)
        {
            if (OpenStation != station) return;
            OpenStation = null;
            SetWalkMode(true);
        }

        void SetWalkMode(bool walking)
        {
            _walking = walking;
            ApplyWalkMode();
        }

        /// <summary>Push the current mode onto the rig. The cursor is set unconditionally —
        /// the FirstPersonController path is an optimisation, not a prerequisite.</summary>
        void ApplyWalkMode()
        {
            if (_fpc == null) _fpc = FindLocalController();
            if (_fpc != null)
            {
                _fpc.MovementLocked = !_walking;
                _fpc.SetCursorLock(_walking);
                return;
            }
            if (!_warnedNoRig)
            {
                _warnedNoRig = true;
                Log.Warn(Tag, "No FirstPersonController found; locking the cursor directly.");
            }
            Cursor.lockState = _walking ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !_walking;
        }

        /// <summary>Registry first (the sanctioned lookup), scene scan as a last resort so a
        /// registration-order surprise cannot leave the hub uncontrollable.</summary>
        static FirstPersonController FindLocalController()
        {
            var local = PlayerRegistry.Local;
            var fpc = local != null ? local.GetComponent<FirstPersonController>() : null;
            return fpc != null ? fpc : FindFirstObjectByType<FirstPersonController>();
        }
    }
}

// wp3-refresh
