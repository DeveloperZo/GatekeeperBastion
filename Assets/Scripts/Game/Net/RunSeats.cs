using System.Collections.Generic;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using Unity.Netcode;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY: which seats the live run is being played by, as distinct from
    /// which clients are connected. The two stop being the same thing the moment a seat can
    /// arrive after the party has embarked: a late arrival is connected, is standing on the
    /// Ship, and must not be counted until it follows the party in at a break.
    ///
    /// Seat changes land on a Prep boundary and nowhere else. Wave composition, the wallet
    /// split and the hatch doors all read the seat count, so letting it move mid-Assault
    /// would resize a wave already on the floor and re-split a pot mid-fight. Admission is
    /// therefore only ever taken in Prep, and a departure is held until the next one.
    ///
    /// Empty is one seat, not zero: solo has no session and no roster, and every consumer
    /// wants "how many are playing" rather than "how many are connected".</summary>
    public static class RunSeats
    {
        #region Constants
        const string Tag = "Seat";
        #endregion

        #region State
        static readonly List<ulong> Admitted = new List<ulong>(WaveDirector.PlayerCapacity);
        static readonly List<ulong> Leaving = new List<ulong>(WaveDirector.PlayerCapacity);
        static bool _live;
        #endregion

        #region Properties
        /// <summary>True while a run is under way and this roster, not the connected list,
        /// is what the seat count means.</summary>
        public static bool IsLive => _live;

        /// <summary>Seats playing the run, clamped to what waves can be composed for. Never
        /// below one — a run with nobody in it is not a state the sim has.</summary>
        public static int Count
        {
            get
            {
                int n = Admitted.Count;
                if (n < 1) return 1;
                if (n > WaveDirector.PlayerCapacity) return WaveDirector.PlayerCapacity;
                return n;
            }
        }

        /// <summary>True when a departure is waiting for the next Prep to be applied.</summary>
        public static bool HasPending => Leaving.Count > 0;
        #endregion

        #region Public API
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            Admitted.Clear();
            Leaving.Clear();
            _live = false;
        }

        public static bool IsAdmitted(ulong clientId)
        {
            return Admitted.Contains(clientId);
        }

        /// <summary>Everyone aboard when the party embarks is in the run. Called as the run
        /// becomes Tethered, so the roster cannot disagree with the seats that embarked.</summary>
        public static void BeginRun(NetworkManager nm)
        {
            Admitted.Clear();
            Leaving.Clear();
            _live = true;
            if (nm == null || !nm.IsServer || !nm.IsListening)
            {
                // Solo. No roster to fill, and Count answers one.
                Log.Event(Tag, "run seats — solo");
                return;
            }
            var clients = nm.ConnectedClientsList;
            for (int i = 0; i < clients.Count; i++)
                Admitted.Add(clients[i].ClientId);
            Log.Event(Tag, "run seats — embarked with " + Admitted.Count);
        }

        public static void EndRun()
        {
            if (!_live) return;
            Admitted.Clear();
            Leaving.Clear();
            _live = false;
            Log.Event(Tag, "run seats — run over, roster cleared");
        }

        /// <summary>Take a seat into the run. Refused when it is already in, so a double
        /// press cannot count one player twice.</summary>
        public static bool Admit(ulong clientId)
        {
            if (!_live)
            {
                Log.Warn(Tag, "admit refused — no run under way clientId=" + clientId);
                return false;
            }
            Leaving.Remove(clientId);
            if (Admitted.Contains(clientId))
            {
                Log.Event(Tag, "admit ignored — already seated clientId=" + clientId);
                return false;
            }
            if (Admitted.Count >= WaveDirector.PlayerCapacity)
            {
                Log.Event(Tag, "admit refused — the run is full clientId=" + clientId);
                return false;
            }
            Admitted.Add(clientId);
            Log.Event(Tag, "seat admitted clientId=" + clientId + " seats=" + Admitted.Count);
            return true;
        }

        /// <summary>A seat has gone. Outside a run it leaves at once; inside one it is held
        /// until the next Prep, because sealing a hatch under attack is a free save and
        /// re-splitting the pot mid-wave pays the survivors for work already done.</summary>
        public static void NoteLeft(ulong clientId)
        {
            if (!_live)
            {
                Admitted.Remove(clientId);
                return;
            }
            if (!Admitted.Contains(clientId))
            {
                // Never followed the party in, so nothing counted it and nothing has to
                // be given back at the boundary.
                Leaving.Remove(clientId);
                return;
            }
            if (Leaving.Contains(clientId)) return;
            Leaving.Add(clientId);
            Log.Event(Tag, "seat left mid-run, held until the break clientId=" + clientId);
        }

        /// <summary>Apply the held departures. Returns true when the roster moved, which is
        /// the caller's signal to recompute the doors and the count.</summary>
        public static bool ApplyBoundary()
        {
            if (!_live || Leaving.Count == 0) return false;
            int before = Admitted.Count;
            for (int i = 0; i < Leaving.Count; i++)
                Admitted.Remove(Leaving[i]);
            Leaving.Clear();
            Log.Event(Tag, "seats at the break — " + before + " to " + Admitted.Count);
            return true;
        }
        #endregion
    }
}
