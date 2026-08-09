using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: Ship-hub scene controller — guarantees a loaded GameSession
    /// (direct-play fallback) and owns cursor/movement state while station panels are open.</summary>
    /// <remarks>Cursor lock is load-bearing: Interactor refuses to raycast unless
    /// Cursor.lockState == Locked, so a missed lock means nothing in the ship is usable.
    /// SetWalkMode therefore always sets the cursor itself (even with no player rig found)
    /// and re-asserts once if the rig registers after Start.</remarks>
    public sealed class ShipController : MonoBehaviour
    {
        const string Tag = "ShipController";
        const float BindWindowSeconds = 2f;

        /// <summary>The station panel currently open, or null while walking.</summary>
        public StationTerminal OpenStation { get; private set; }

        /// <summary>True while any station panel is up (read by ShipHud).</summary>
        public bool IsPanelOpen => OpenStation != null;

        FirstPersonController _fpc;
        bool _walking = true;
        bool _warnedNoRig;
        float _bindWindow = BindWindowSeconds;

        void Awake()
        {
            // Editor-direct-play / test fallback: entering Ship without passing the menu.
            GameSession.EnsureLoaded();
        }

        void Start()
        {
            SetWalkMode(true);
            if (GameSession.Meta != null)
                GameSession.Meta.PayloadLoadoutLocked = false;
            // A cleared run bumps CleanExits, not PodEjections, so "run(s) lost" stayed honest —
            // it just read as if a won run had never happened. Both counters, both true.
            Log.Event("Ship", $"aboard the ship — {GameSession.Meta.BankedSalvage} banked, " +
                                        $"{GameSession.Meta.PodEjections} run(s) lost, " +
                                        $"{GameSession.Meta.CleanExits} cleared");
        }

        void Update()
        {
            // Re-assert exactly once, if the player rig only showed up after our Start:
            // PlayerRegistry is filled from PlayerHealth.OnEnable, and nothing guarantees
            // that ran first. Bounded so we never fight the player's own Esc/click later.
            if (_fpc != null || _bindWindow <= 0f) return;
            _bindWindow -= Time.deltaTime;
            var fpc = FindLocalController();
            if (fpc == null) return;
            _fpc = fpc;
            ApplyWalkMode();
        }

        /// <summary>Open a station panel: cursor freed, movement locked. One panel at a time.</summary>
        public void OpenStationPanel(StationTerminal station)
        {
            if (station == null || OpenStation == station) return;
            OpenStation = station;
            SetWalkMode(false);
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
