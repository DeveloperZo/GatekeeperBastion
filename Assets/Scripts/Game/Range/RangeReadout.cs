using System.Text;
using GatekeepersProtocol.Util;
using TMPro;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>What the lane just told you: shots, damage, time-to-kill on the last body, and
    /// what every cooldown is doing.
    ///
    /// It OBSERVES. Shots come off <see cref="PlayerGun.Fired"/>; damage is read as the drop in
    /// each live body's Hp between frames; cooldowns are polled off the abilities. Nothing here
    /// is counted inside <c>Sim/</c> — a counters-and-rollups layer there is on the rejected list
    /// and would outlive the range by years (Docs/Design/3-Mechanics.md).
    ///
    /// Reading Hp deltas rather than a damage event is deliberate: <c>Damageable.Damaged</c>
    /// carries the attacker and not the amount, and widening it for a dev tool would put the
    /// range in the damage pipeline of every fight in the game.</summary>
    public sealed class RangeReadout : MonoBehaviour
    {
        const string Tag = "Range";

        /// <summary>Repaint interval. The numbers are read every frame; the STRING is not, because
        /// building it is the only allocation here.</summary>
        const float RepaintSeconds = 0.15f;

        [SerializeField, Tooltip("Where the readout is drawn. Authored in Range.unity.")]
        TMP_Text _text;

        [SerializeField, Tooltip("The range's GameController.")]
        GameController _game;

        [SerializeField, Tooltip("The range's rules, for the toggle states.")]
        RangeRules _rules;

        [SerializeField, Tooltip("The board - its line is the readout's third row. Authored by the range builder.")]
        RangeBoard _board;

        readonly StringBuilder _sb = new StringBuilder(256);
        readonly System.Collections.Generic.Dictionary<int, float> _lastHp =
            new System.Collections.Generic.Dictionary<int, float>(8);

        PlayerGun _gun;
        int _shots;
        float _damage;
        float _lastTtk = -1f;
        float _firstHitAt = -1f;
        float _repaintAt;

        public int Shots { get { return _shots; } }
        public float Damage { get { return _damage; } }

        /// <summary>Seconds from the first hit landed to the last body dropping, or -1 before
        /// anything has died since the last clear.</summary>
        public float LastTimeToKill { get { return _lastTtk; } }

        public void Bind(GameController game, RangeRules rules, TMP_Text text)
        {
            _game = game;
            _rules = rules;
            _text = text;
        }

        /// <summary>Back to nothing measured. Called when the lane is cleared, because a
        /// time-to-kill carried over from a kit you are no longer holding is a lie.</summary>
        public void ResetCounters()
        {
            _shots = 0;
            _damage = 0f;
            _lastTtk = -1f;
            _firstHitAt = -1f;
            _lastHp.Clear();
            Log.Event(Tag, "readout cleared");
        }

        void OnDisable()
        {
            if (_gun == null) return;
            _gun.Fired -= OnFired;
            _gun = null;
        }

        void OnFired()
        {
            _shots++;
        }

        void Update()
        {
            BindGunIfNeeded();
            SampleDamage();
            if (Time.unscaledTime < _repaintAt) return;
            _repaintAt = Time.unscaledTime + RepaintSeconds;
            Repaint();
        }

        /// <summary>The rig is rebuilt whenever the kit changes, so the gun this was subscribed
        /// to can be replaced under it. Re-resolve rather than assume.</summary>
        void BindGunIfNeeded()
        {
            var game = _game;
            if (game == null) return;
            PlayerGun current = game.Gun;
            if (current == _gun) return;
            if (_gun != null) _gun.Fired -= OnFired;
            _gun = current;
            if (_gun != null) _gun.Fired += OnFired;
        }

        /// <summary>Damage is the drop in Hp across the bodies the spawner is holding. A body
        /// that dies between samples contributes whatever it had left — otherwise the killing
        /// blow, the only one anybody cares about, would be the one that never counted.</summary>
        void SampleDamage()
        {
            var game = _game;
            if (game == null || game.Spawner == null) return;
            var live = game.Spawner.Live;

            for (int i = 0; i < live.Count; i++)
            {
                EnemyGatekeeper body = live[i];
                if (body == null) continue;
                var hp = body.GetComponent<Damageable>();
                if (hp == null) continue;

                int id = body.GetInstanceID();
                float now = hp.Hp;
                float before;
                if (!_lastHp.TryGetValue(id, out before))
                {
                    _lastHp[id] = now;
                    continue;
                }
                if (now < before)
                {
                    float dealt = before - now;
                    _damage += dealt;
                    if (_firstHitAt < 0f) _firstHitAt = Time.time;
                    if (hp.IsDead && _firstHitAt >= 0f)
                    {
                        _lastTtk = Time.time - _firstHitAt;
                        _firstHitAt = -1f;
                    }
                }
                _lastHp[id] = now;
            }
        }

        void Repaint()
        {
            var text = _text;
            if (text == null) return;

            _sb.Length = 0;
            _sb.Append("SHOTS ").Append(_shots);
            _sb.Append("   DAMAGE ").Append(Mathf.RoundToInt(_damage));
            _sb.Append("   TTK ");
            if (_lastTtk < 0f) _sb.Append('-');
            else _sb.Append(_lastTtk.ToString("0.00")).Append('s');

            var game = _game;
            if (game != null)
            {
                var ability = game.Ability;
                if (ability != null)
                    AppendCooldown("WEAPON", ability.CooldownRemaining);
                var protocol = game.Protocol;
                if (protocol != null)
                {
                    AppendCooldown("CREW", protocol.CrewCooldown);
                    AppendCooldown("SELF", protocol.PersonalCooldown);
                }
            }

            var rules = _rules;
            var board = _board;
            if (board != null)
            {
                // The board's keys beside their values: the range's verbs are read off this
                // line, not remembered (M117, Zo 2026-09-03). The toggle states ride on it.
                _sb.Append('\n').Append(board.Line);
                if (rules != null && rules.PendingRespawns > 0)
                    _sb.Append(" (").Append(rules.PendingRespawns).Append(" returning)");
            }
            else if (rules != null)
            {
                // The toggle states belong on screen, not only in the log — criterion 4 asks for
                // the bypass to be readable, and a range that silently stopped bypassing would
                // look like an ability that had gone wrong.
                _sb.Append("\nCOOLDOWNS ").Append(rules.CooldownBypass ? "BYPASSED" : "REAL");
                _sb.Append("   RESPAWN ").Append(rules.RespawnOn ? "ON" : "OFF");
                if (rules.PendingRespawns > 0)
                    _sb.Append(" (").Append(rules.PendingRespawns).Append(" returning)");
            }

            text.text = _sb.ToString();
        }

        void AppendCooldown(string label, float remaining)
        {
            _sb.Append("   ").Append(label).Append(' ');
            if (remaining <= 0f) _sb.Append("READY");
            else _sb.Append(remaining.ToString("0.0")).Append('s');
        }
    }
}
