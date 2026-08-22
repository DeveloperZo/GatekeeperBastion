using System.Collections.Generic;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>The two rules that make a range a range rather than a fight: abilities need not
    /// wait, and bodies come back.
    ///
    /// The cooldown bypass clears the timer from out here every frame rather than branching
    /// inside <see cref="PlayerAbility"/>. That is the milestone's stated trap: an ability that
    /// knows it is in a range is not the ability that ships, and the range would then be
    /// measuring a copy of the mechanism. Clearing the timer leaves the shipped path exactly as
    /// it is — it simply never finds itself waiting.
    ///
    /// The bypass is a toggle and not a fixture, because how long an ability makes you wait is
    /// itself one of the things being judged.
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

        bool _bypass = true;

        /// <summary>True while abilities fire as fast as they can be pressed. Default, because
        /// that is what practice needs; one input restores the real wait.</summary>
        public bool CooldownBypass
        {
            get { return _bypass; }
        }

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

        /// <summary>Flip the bypass and say so — the toggle state has to be readable, and the
        /// log is the half of that which survives into a bug report.</summary>
        public bool ToggleCooldownBypass()
        {
            _bypass = !_bypass;
            Log.Event(Tag, "cooldown bypass " + (_bypass ? "on — abilities do not wait"
                : "off — real cooldowns"));
            return _bypass;
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

        void Update()
        {
            if (!_bypass) return;
            var game = _game;
            if (game == null) return;

            // Every frame rather than on press: an ability can start a cooldown from a path
            // this object never sees (a hit registering, a window closing), and the player
            // must not find a bypassed ability refusing them anyway.
            var ability = game.Ability;
            if (ability != null) ability.ClearCooldown();
            var protocol = game.Protocol;
            if (protocol != null) protocol.ClearCooldowns();
        }
    }
}
