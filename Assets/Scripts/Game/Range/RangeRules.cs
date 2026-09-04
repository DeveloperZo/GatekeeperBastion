using System.Collections.Generic;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>What the range does to the abilities, and the respawn queue the standing
    /// ledger used to feed.
    ///
    /// No cooldown bypass (Zo, 2026-09-03: "Cooldown - no bypass. reset them on wave start").
    /// M99's bypass cleared every timer every frame; it judged an ability at a wait nobody
    /// ships. Instead the timers are cleared once, from out here, when a wave is drawn — the
    /// same reset-from-outside shape, so <see cref="PlayerAbility"/> never learns it is on a
    /// range — and every wave is met with a full kit at the shipped wait.
    ///
    /// Bodies are not owned here. This holds the queue and answers "who is due"; whoever
    /// spawned an actor is the thing that can put it back
    /// (Docs/Milestones — a range you can fight in).</summary>
    public sealed class RangeRules : MonoBehaviour
    {
        const string Tag = "Range";

        [SerializeField, Tooltip("The range's GameController. The abilities are resolved from it "
            + "rather than looked up in the scene.")]
        GameController _game;

        readonly RespawnQueue _respawn = new RespawnQueue();

        bool _missionDrawerClosed;

        public bool RespawnOn
        {
            get { return _respawn.Enabled; }
        }

        public float RespawnDelay
        {
            get { return _respawn.Delay; }
            set { _respawn.Delay = value; }
        }

        public int PendingRespawns
        {
            get { return _respawn.PendingCount; }
        }

        public void Bind(GameController game)
        {
            _game = game;
        }

        /// <summary>Every ability timer to zero, once — the board calls it as a wave is drawn.
        /// From out here rather than inside the ability, for the reason the bypass was: the
        /// ability that ships must not know it is on a range.</summary>
        public void ResetCooldowns()
        {
            var game = _game;
            if (game == null) return;
            var ability = game.Ability;
            if (ability != null) ability.ClearCooldown();
            var protocol = game.Protocol;
            if (protocol != null) protocol.ClearCooldowns();
            Log.Event(Tag, "cooldowns reset — wave start");
        }

        public bool ToggleRespawn()
        {
            _respawn.Enabled = !_respawn.Enabled;
            if (!_respawn.Enabled) _respawn.Clear();
            Log.Event(Tag, "respawn " + (_respawn.Enabled
                ? "on — bodies return after " + _respawn.Delay.ToString("0.#") + "s"
                : "off"));
            return _respawn.Enabled;
        }

        /// <summary>A body went down. Returns true when it is queued to come back, which is
        /// false whenever respawn is off.</summary>
        public bool NoteDown(int bodyId)
        {
            return _respawn.NoteDown(bodyId, Time.time);
        }

        /// <summary>Ids whose delay has elapsed. The list is reused between calls — read it
        /// before calling again.</summary>
        public List<int> TakeDueRespawns()
        {
            return _respawn.TakeDue(Time.time);
        }

        public void ForgetBody(int bodyId)
        {
            _respawn.Forget(bodyId);
        }

        /// <summary>Clearing the lane clears what was coming back to it.</summary>
        public void ClearLane()
        {
            _respawn.Clear();
        }

        /// <summary>Put the Active Mission drawer away, once.
        ///
        /// A range is Tethered from Start like any Bastion — GameController.Start engages the
        /// tether — so HudController runs its once-per-Run "Prep opens the mission drawer", and
        /// a lane that has no mission gets a PREP panel it never asked for. This waits for that
        /// auto-open and closes it through the same Controls flag the player's own toggle uses,
        /// so the drawer is closed rather than suppressed: the toggle still opens it.</summary>
        void CloseMissionDrawerOnce()
        {
            if (_missionDrawerClosed) return;
            if (!Controls.MissionDrawerOpen) return;   // not opened yet; wait for the Prep frame
            _missionDrawerClosed = true;
            Controls.SetMissionDrawerOpen(false);
            Log.Event(Tag, "mission drawer closed — a lane has no mission");
        }

        void Update()
        {
            CloseMissionDrawerOnce();
        }
    }
}
