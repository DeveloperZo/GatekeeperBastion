using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using Michsky.UI.Shift;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: Ship-hub scene controller — guarantees a loaded GameSession
    /// (direct-play fallback), owns cursor/movement while station panels are open, and is the
    /// sole Esc dispatcher for the Ship (Escape contract).</summary>
    /// <remarks>Cursor lock is load-bearing in first person: Interactor refuses to raycast
    /// unless Cursor.lockState == Locked, so a missed lock means nothing in the ship is
    /// usable. SetWalkMode therefore always sets the cursor itself (even with no player rig
    /// found) and re-asserts once if the rig registers after Start. With the isometric rig
    /// active the invariant flips: walking must leave the cursor FREE (cursor-driven aim),
    /// and Interactor accepts the free cursor there.</remarks>
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
        [SerializeField, OptionalBinding] GameObject _embarkPauseButton;
        [SerializeField, OptionalBinding, Tooltip("Authored CompendiumView on the Ship pause shell.")]
        CompendiumView _compendium;

        [SerializeField, OptionalBinding, Tooltip("Isometric rig (Ship probe). While wired and " +
            "enabled, walk mode leaves the cursor free (aim and interact are cursor-driven) and " +
            "station targeting uses the cursor ray; null or disabled means first-person " +
            "behavior, unchanged.")]
        IsometricCameraRig _isoRig;

        /// <summary>The station panel currently open, or null while walking.</summary>
        public StationTerminal OpenStation { get; private set; }

        /// <summary>True while any station panel is up (read by ShipHud).</summary>
        public bool IsPanelOpen => OpenStation != null;

        /// <summary>True while the Settings panel is up via Esc / OptionsScreen.</summary>
        public bool IsSettingsOpen { get; private set; }

        /// <summary>QUIT TO MENU on the Ship Settings panel — state is
        /// already saved at purchase time; no confirm. Active session shuts down.</summary>
        public void QuitToMenu()
        {
            var session = NetSession.FindLive();
            if (session != null && session.Active)
                session.ShutdownSession();
            SceneFlow.LoadMainMenu();
        }

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
            LevelMusic.CaptureOrJoin(null);
            SetWalkMode(true);
            // Direct-play/transition belt: embarking locks the loadout; returning to Ship unlocks it.
            // A down-wait is still a live Run — do not reopen the shop.
            if (!NetShipWait.IsActive && GameSession.Meta != null)
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
            // Target is the console under the camera ray, not a hard-coded Armory. The demo
            // gate goes first: dark refuses, the rack grant and the tap-heal all consume the
            // Down before any panel; with the demo off it always falls through.
            if (Controls.Down(ControlId.Interact) && !DemoVoice.LocksInput)
            {
                var target = ResolveTargetedStation();
                if (!DemoStationGate.TryHandleInteractDown(target))
                    TryOpenStationFromInteract(target);
            }

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
            // A live Lock line owns the screen: no station opens under it. The cursor is
            // already free (DemoVoiceView), so the prompt is hidden anyway — silent refuse.
            if (DemoVoice.LocksInput) return false;
            if (station == null || OpenStation != null || IsSettingsOpen || OptionsScreen.IsOpen)
                return false;
            if (NetShipWait.IsActive && !(station is MapStation))
            {
                Log.Event("Net", "station refused — live Run " + station.StationName);
                return false;
            }
            OpenStationPanel(station);
            return true;
        }

        /// <summary>Pause EMBARK — Hatch-bank get-up, or party Ready / Cancel countdown.</summary>
        public void TryEmbarkFromPause()
        {
            if (NetShipWait.IsActive)
            {
                var avatar = NetPlayerAvatar.LocalOwner;
                if (avatar == null)
                {
                    Log.Event("Net", "embark refused — no avatar");
                    return;
                }
                avatar.RequestEmbark();
                return;
            }

            var session = NetSession.FindLive();
            if (session == null || !session.Active || session.Lobby == null)
            {
                Log.Event("Net", "pause Ready refused — no party lobby");
                return;
            }
            if (session.Lobby.IsCountingDown)
            {
                session.Lobby.CancelCountdown();
                return;
            }
            var local = NetPlayerAvatar.LocalOwner;
            if (local == null)
            {
                Log.Event("Net", "ready refused — no avatar");
                return;
            }
            session.Lobby.SetReady(!local.LobbyReady.Value);
        }

        /// <summary>Camera-forward ray, same range as <see cref="Interactor"/> — self-contained
        /// so open does not depend on Interactor script order. The ray comes from the local
        /// rig's authored camera wire, never a scene-wide camera lookup. Iso mode targets the
        /// console under the cursor instead, still requiring the player within InteractRange.</summary>
        StationTerminal ResolveTargetedStation()
        {
            if (_isoRig != null && _isoRig.isActiveAndEnabled)
            {
                var local = PlayerRegistry.Local;
                if (local == null) return null;
                // Pointer comes from the input producer's frame, never an ambient engine poll.
                var frame = Controls.Sample();
                Ray ray = _isoRig.ScreenRay(new Vector3(frame.pointerX, frame.pointerY, 0f));
                // Cursor ray starts at the camera, FollowDistance back — cast long, then
                // keep the walk-up contract by distance from the player, not the camera.
                if (!Physics.Raycast(ray, out RaycastHit cursorHit,
                        ShipCamera.FollowDistance * 2f))
                    return null;
                if ((cursorHit.point - local.transform.position).sqrMagnitude
                        > InteractRange * InteractRange)
                    return null;
                return cursorHit.collider.GetComponentInParent<StationTerminal>();
            }

            var fpc = FindLocalController();
            var cam = fpc != null ? fpc.cameraTransform : null;
            if (cam == null) return null;
            if (!Physics.Raycast(cam.position, cam.forward, out RaycastHit hit,
                    InteractRange))
                return null;
            return hit.collider.GetComponentInParent<StationTerminal>();
        }

        void LateUpdate()
        {
            // Esc is FreeCursor on Ship; FirstPersonController also unlocks on that binding.
            // After the Escape contract, walking means locked — re-assert once per frame so FPC
            // cannot leave the hub cursor-free after Esc opened/closed Settings or a station.
            if (_walking && OpenStation == null && !IsSettingsOpen && !OptionsScreen.IsOpen
                && !DemoVoice.LocksInput)
                ApplyWalkMode();
        }

        /// <summary>Ship Esc priority: open modal → close; station open → Close; Options tabs open
        /// → back to Pause Menu; Pause Menu open → close; otherwise → open Pause Menu (Escape
        /// contract).</summary>
        void HandleEscape()
        {
            // A live demo line is topmost: Esc ends a Lock line (same as Continue) or
            // dismisses a Subtitle, and is consumed — it must not also open Settings.
            if (DemoVoice.TryHandleEscape()) return;

            if (_exitModal != null && _exitModal.gameObject.activeInHierarchy)
            {
                _exitModal.ModalWindowOut();
                return;
            }

            if (OpenStation != null)
            {
                if (OpenStation.TryBack()) return;
                OpenStation.Close();
                return;
            }

            if (OptionsScreen.IsOpen)
            {
                BackToPauseMenu();
                return;
            }

            if (_compendium != null && _compendium.IsOpen)
            {
                _compendium.Close();
                return;
            }

            if (IsSettingsOpen)
            {
                CloseSettings();
                return;
            }

            OpenSettings();
        }

        /// <summary>Wired from the pause Compendium button — reader stays under pause input ownership.</summary>
        public void OpenCompendium()
        {
            if (!IsSettingsOpen) OpenSettings();
            if (_compendium == null)
            {
                Log.Error(Tag, "CompendiumView not authored on Ship pause.");
                return;
            }
            _compendium.Open();
        }

        void OpenSettings()
        {
            HideStationPanels();
            if (_stationCanvas != null && !_stationCanvas.activeSelf) _stationCanvas.SetActive(true);
            if (_settingsPanel != null && _settingsPanel.activeSelf) _settingsPanel.SetActive(false);
            if (_pauseMenuPanel != null) _pauseMenuPanel.SetActive(true);
            if (_embarkPauseButton != null)
            {
                bool wait = NetShipWait.IsActive;
                var session = NetSession.FindLive();
                bool partyGuest = !wait && session != null && session.Active && !session.IsPartyLead;
                bool partyCountdown = !wait && session != null && session.Lobby != null
                    && session.Lobby.IsCountingDown;
                _embarkPauseButton.SetActive(wait || partyGuest || partyCountdown);
            }
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

        /// <summary>Station panels share this canvas and ARMORY is authored active (edit-mode
        /// preview). Hide them so pause is the only Main Panels body that paints.</summary>
        void HideStationPanels()
        {
            if (_panels == null) return;
            var list = _panels.panels;
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                var go = list[i].panelObject;
                if (go == null || go == _pauseMenuPanel) continue;
                if (go.activeSelf) go.SetActive(false);
            }
        }

        /// <summary>Resume button on the Pause Menu shell — same as pressing Esc from the shell.</summary>
        public void ResumeFromPauseMenu() => CloseSettings();

        void CloseSettings()
        {
            if (_compendium != null && _compendium.IsOpen)
                _compendium.Close();
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
            var boarder = _fpc != null ? _fpc.GetComponent<BoarderPresentation>() : null;
            if (boarder != null) boarder.SetStationHidden(true);
        }

        /// <summary>Close the open panel (if it is the given one) and return to walking.</summary>
        public void CloseStationPanel(StationTerminal station)
        {
            if (OpenStation != station) return;
            OpenStation = null;
            SetWalkMode(true);
            var boarder = _fpc != null ? _fpc.GetComponent<BoarderPresentation>() : null;
            if (boarder != null) boarder.SetStationHidden(false);
        }

        void SetWalkMode(bool walking)
        {
            _walking = walking;
            ApplyWalkMode();
        }

        /// <summary>Push the current mode onto the rig. The cursor is set unconditionally —
        /// the FirstPersonController path is an optimisation, not a prerequisite. Iso mode
        /// never locks: the OS cursor is the aim point, walking or not.</summary>
        void ApplyWalkMode()
        {
            if (_fpc == null) _fpc = FindLocalController();
            bool lockCursor = _walking && !(_isoRig != null && _isoRig.isActiveAndEnabled);
            if (_fpc != null)
            {
                _fpc.MovementLocked = !_walking;
                _fpc.SetCursorLock(lockCursor);
                return;
            }
            if (!_warnedNoRig)
            {
                _warnedNoRig = true;
                Log.Warn(Tag, "No FirstPersonController found; locking the cursor directly.");
            }
            Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lockCursor;
        }

        /// <summary>Registry only (the sanctioned lookup); the bind window in Update retries
        /// until the rig registers, so a registration-order surprise resolves itself.</summary>
        static FirstPersonController FindLocalController()
        {
            var local = PlayerRegistry.Local;
            return local != null ? local.GetComponent<FirstPersonController>() : null;
        }
    }
}

// wp3-refresh
