using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: FORGE's hands on deployed hardware — the FIELD REARM channel and
    /// the REDEPLOY carry. Both verbs act on the same machine and hold state across frames,
    /// which is why they share a type and neither lives in
    /// <see cref="PlayerProtocolAbilities"/>: that owns windows and input, this owns the
    /// machine. Reaches its bay through the bound controller — no scene lookup.</summary>
    public sealed class PlayerHardwareHandling : MonoBehaviour
    {
        const string Tag = "Protocol";

        /// <summary>Carried machines ride at chest height in front of the Boarder.</summary>
        static readonly Vector3 CarryOffset = new Vector3(0f, 0.9f, 0.9f);

        GameController _game;
        FirstPersonController _fpc;
        PlayerGun _gun;

        bool _carrying;
        bool _channelling;
        float _spendAcc;
        long _spentThisChannel;
        float _repairedThisChannel;

        public bool Carrying => _carrying;
        public bool Channelling => _channelling;

        public void Bind(GameController game)
        {
            _game = game;
            if (_fpc == null) _fpc = GetComponent<FirstPersonController>();
            if (_gun == null) _gun = GetComponentInChildren<PlayerGun>();
        }

        DecoyBay Bay => _game != null ? _game.Decoys : null;

        /// <summary>The machine this Boarder may put hands on: their own bay's live prop,
        /// within reach. Null with a spoken reason otherwise.</summary>
        DecoyObject ReachableMachine(out string reason)
        {
            var bay = Bay;
            if (bay == null)
            {
                reason = "no drone bay";
                return null;
            }
            var live = bay.Live;
            if (live == null || !live.IsAlive || !bay.State.IsLive)
            {
                reason = "no machine deployed";
                return null;
            }
            float d = Vector3.Distance(transform.position, live.transform.position);
            if (d > BoarderProtocol.FieldRearmReachMetres)
            {
                reason = "machine out of reach (" + d.ToString("0.#") + "m)";
                return null;
            }
            reason = null;
            return live;
        }

        // ---------- FIELD REARM ----------

        /// <summary>Open the channel. False (with the reason logged) when there is nothing in
        /// reach to feed, or nothing unbanked to feed it with.</summary>
        public bool TryBeginRearm()
        {
            string reason;
            var machine = ReachableMachine(out reason);
            if (machine == null)
            {
                Log.Event(Tag, "crew Field Rearm refused — " + reason);
                return false;
            }
            var sim = _game != null ? _game.Sim : null;
            if (sim == null || sim.Unbanked <= 0L)
            {
                Log.Event(Tag, "crew Field Rearm refused — nothing unbanked to spend");
                return false;
            }
            if (machine.Hp >= machine.MaxHp)
            {
                Log.Event(Tag, "crew Field Rearm refused — machine already at full life");
                return false;
            }
            _channelling = true;
            _spendAcc = 0f;
            _spentThisChannel = 0L;
            _repairedThisChannel = 0f;
            if (_fpc != null) _fpc.Rooted = true;
            return true;
        }

        /// <summary>Pour unbanked into the machine for this frame. Ends itself when the pile
        /// runs dry, the machine fills, or it leaves reach — the window does not have to expire
        /// for the channel to be over.</summary>
        public void TickRearm(float dt)
        {
            if (!_channelling || dt <= 0f) return;
            var sim = _game != null ? _game.Sim : null;
            var bay = Bay;
            if (sim == null || bay == null)
            {
                EndRearm("bay lost");
                return;
            }
            string reason;
            var machine = ReachableMachine(out reason);
            if (machine == null)
            {
                EndRearm(reason);
                return;
            }

            // Salvage is a whole-unit currency; carry the fraction rather than rounding it away
            // every frame, or a 60 fps channel spends less than a 30 fps one.
            _spendAcc += BoarderProtocol.FieldRearmSalvagePerSecond * dt;
            long want = (long)_spendAcc;
            if (want <= 0L) return;
            _spendAcc -= want;

            long spent = sim.SpendUnbanked(0, want);
            if (spent <= 0L)
            {
                EndRearm("out of unbanked");
                return;
            }
            _spentThisChannel += spent;

            float restored = bay.State.Repair(spent * BoarderProtocol.FieldRearmHpPerSalvage);
            _repairedThisChannel += restored;
            machine.SyncHp();
            if (restored <= 0f) EndRearm("machine at full life");
        }

        /// <summary>Close the channel and speak what it bought. Safe to call when shut.</summary>
        public void EndRearm(string why)
        {
            if (!_channelling) return;
            _channelling = false;
            _spendAcc = 0f;
            if (_fpc != null) _fpc.Rooted = false;
            Log.Event(Tag, "crew Field Rearm ended — spent " + _spentThisChannel
                + " unbanked for " + _repairedThisChannel.ToString("0.#") + " HP ("
                + (why != null ? why : "window end") + ")");
        }

        // ---------- REDEPLOY ----------

        /// <summary>Take the machine into both hands. False (with the reason logged) when
        /// nothing deployed is in reach.</summary>
        public bool TryLift()
        {
            if (_carrying) return false;
            string reason;
            var machine = ReachableMachine(out reason);
            if (machine == null)
            {
                Log.Event(Tag, "self Redeploy refused — " + reason);
                return false;
            }
            _carrying = true;
            var t = machine.transform;
            t.SetParent(transform, false);
            t.localPosition = CarryOffset;
            t.localRotation = Quaternion.identity;
            ApplyCarryLoad(true);
            Log.Event(Tag, "self Redeploy — lifted a machine at "
                + machine.Hp.ToString("0") + "/" + machine.MaxHp.ToString("0") + " HP");
            return true;
        }

        /// <summary>Set the machine down where the Boarder stands. It keeps the life it had —
        /// this is a move, not a fresh placement, and spends no charge.</summary>
        public bool TrySetDown()
        {
            if (!_carrying) return false;
            _carrying = false;
            ApplyCarryLoad(false);

            var bay = Bay;
            var machine = bay != null ? bay.Live : null;
            if (bay == null || machine == null)
            {
                Log.Event(Tag, "self Redeploy — the machine was lost while carried");
                return false;
            }

            Vector3 at = transform.position;
            string reason;
            if (!bay.State.TryRelocate(at.x, at.y, at.z, out reason))
            {
                Log.Event(Tag, "self Redeploy refused — " + reason);
                return false;
            }
            var t = machine.transform;
            t.SetParent(null, true);
            t.position = at + Vector3.up * 0.6f;
            t.rotation = Quaternion.identity;
            Log.Event(Tag, "self Redeploy — set down at "
                + machine.Hp.ToString("0") + "/" + machine.MaxHp.ToString("0") + " HP");
            return true;
        }

        /// <summary>Window expiry: the machine goes down wherever the Boarder is standing.
        /// A carried machine is never left in hand across the window boundary.</summary>
        public void ForceSetDown()
        {
            if (!_carrying) return;
            TrySetDown();
        }

        void ApplyCarryLoad(bool full)
        {
            if (_fpc != null)
                _fpc.SpeedMul = full ? BoarderProtocol.RedeployCarrySpeedMul : 1f;
            if (_gun == null) _gun = GetComponentInChildren<PlayerGun>();
            if (_gun != null) _gun.HandsFull = full;
        }

        void OnDisable()
        {
            // Never strand the load on a disabled rig: a carried machine's slow and disarm
            // would outlive the carry and read as a broken gun.
            ApplyCarryLoad(false);
            _carrying = false;
            _channelling = false;
            if (_fpc != null) _fpc.Rooted = false;
        }
    }
}
