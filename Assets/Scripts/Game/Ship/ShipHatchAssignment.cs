using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using Unity.Netcode;

namespace GatekeepersProtocol.Game
{
    /// <summary>HOST-ONLY command layer for the hatch doors: recomputes the seat-count
    /// assignment (ShipHatches), writes it to the run mirror for every seat, and logs one
    /// "assigned" line per seat. Called on Ship load, on party change and on the embark
    /// commit. Solo has no session and derives the one-seat assignment locally — doors
    /// read <see cref="CurrentPacked"/> and never write. Not GameController's job: that
    /// binding is Bastion-only and null on the Ship, where the doors stand.</summary>
    public static class ShipHatchAssignment
    {
        const string Tag = "Hatch";

        /// <summary>No client carries this id; used when no seat is being excluded.</summary>
        const ulong ExcludeNone = ulong.MaxValue;

        static readonly int SoloPacked = ShipHatches.PackFor(new[] { 0 }, 1);

        /// <summary>Packed assignment for door presentation. Solo derives the one-seat
        /// form; a session reads the host-written mirror (0 — all sealed — until the
        /// host first assigns).</summary>
        public static int CurrentPacked
        {
            get
            {
                var session = NetSession.FindLive();
                if (session == null || !session.Active) return SoloPacked;
                var mirror = session.Mirror;
                if (mirror == null) return 0;
                return mirror.HatchAssign.Value;
            }
        }

        /// <summary>Recompute from the current seat count. Solo logs the one seat; the
        /// party lead writes the mirror; a guest is refused (doors read, never write).</summary>
        public static void Recompute(string reason)
        {
            Recompute(reason, ExcludeNone);
        }

        /// <summary>Recompute while a seat is leaving: the disconnect callback can fire
        /// while the leaver is still in the connected list, so it is excluded by id.</summary>
        public static void Recompute(string reason, ulong excludeClientId)
        {
            var session = NetSession.FindLive();
            if (session == null || !session.Active)
            {
                Log.Event(Tag, "recompute reason=" + reason + " seats=1 (solo)");
                LogAssignment(SoloPacked, 1);
                return;
            }
            if (!session.IsPartyLead)
            {
                Log.Event(Tag, "recompute refused — not party lead (" + reason + ")");
                return;
            }
            ServerRecompute(NetworkManager.Singleton, session.Mirror, reason, excludeClientId);
        }

        /// <summary>Host core, callable from a fixture with its own manager and mirror.
        /// Returns the packed assignment it wrote (0 when refused).</summary>
        public static int ServerRecompute(
            NetworkManager nm, NetRunMirror mirror, string reason, ulong excludeClientId = ExcludeNone)
        {
            if (nm == null || !nm.IsServer)
            {
                Log.Warn(Tag, "recompute refused — no server manager (" + reason + ")");
                return 0;
            }

            var slots = new int[ShipHatches.Count];
            int seats = GatherSeatSlots(nm, excludeClientId, slots);
            int packed = ShipHatches.PackFor(slots, seats);

            if (mirror != null)
                mirror.ServerSetHatchAssign(packed);
            else
                Log.Warn(Tag, "recompute — no mirror to replicate (" + reason + ")");

            Log.Event(Tag, "recompute reason=" + reason + " seats=" + seats);
            LogAssignment(packed, seats);
            return packed;
        }

        static void LogAssignment(int packed, int seats)
        {
            for (int i = 0; i < ShipHatches.Count; i++)
            {
                int owner = ShipHatches.OwnerSlot(packed, i);
                if (owner < 0) continue;
                Log.Event(Tag, "assigned slot=" + owner + " hatch=" + ShipHatches.IdOf(i)
                    + " seats=" + seats);
            }
        }

        /// <summary>One seat per connected client, capped at the hatch count; slot from
        /// the seat's avatar. A seat with no avatar yet (a just-approved join, or a
        /// fixture without player avatars) falls back to connection order — the next
        /// party-change recompute corrects it. Ascending on return.</summary>
        static int GatherSeatSlots(NetworkManager nm, ulong excludeClientId, int[] slots)
        {
            var clients = nm.ConnectedClientsList;
            int seats = 0;
            for (int i = 0; i < clients.Count && seats < ShipHatches.Count; i++)
            {
                var client = clients[i];
                if (client.ClientId == excludeClientId) continue;
                int slot = -1;
                var po = client.PlayerObject;
                if (po != null)
                {
                    var avatar = po.GetComponent<NetPlayerAvatar>();
                    if (avatar != null) slot = avatar.Slot.Value;
                }
                if (slot < 0 || slot >= ShipHatches.Count) slot = seats;
                slots[seats] = slot;
                seats++;
            }
            if (seats == 0)
            {
                // A listening server always seats its own client; keep the middle door
                // open rather than sealing the ship if the list is briefly empty.
                slots[0] = 0;
                seats = 1;
            }
            System.Array.Sort(slots, 0, seats);
            return seats;
        }
    }
}
