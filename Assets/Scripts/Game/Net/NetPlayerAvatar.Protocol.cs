using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using Unity.Netcode;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER (partial of NetPlayerAvatar): protocol/passive equip push and
    /// owner-rig bind. Split so the avatar file stays under cap.</summary>
    public sealed partial class NetPlayerAvatar
    {
        public override void OnGainedOwnership()
        {
            base.OnGainedOwnership();
            if (!IsSpawned || !IsOwner) return;
            BindOwnerRig();
            ReportOwnerMeta();
        }

        void BindOwnerRig()
        {
            var found = FindAnyObjectByType<FirstPersonController>();
            if (found != null && found == _rig) return;
            if (found == null && _rig == null) return;
            _spawnOffsetApplied = false;
            _rig = found;
            _rigHealth = _rig != null ? _rig.GetComponent<PlayerHealth>() : null;
            OffsetOwnerRig();
            if (_rig != null)
                Log.Event(Tag, "owner rig bound clientId=" + OwnerClientId);
        }

        void ReportOwnerMeta()
        {
            int deepest = GameSession.Meta != null ? GameSession.Meta.DeepestWaveReached : 1;
            ReportDeepestServerRpc(deepest);
            var meta = GameSession.Meta;
            int proto = meta != null ? (int)meta.EquippedProtocol : (int)BoarderProtocolId.Default;
            int passive = meta != null ? (int)meta.EquippedPassive : (int)BoarderAbilityId.Ghost;
            ReportProtocolServerRpc(proto, passive);
        }

        [Rpc(SendTo.Server)]
        void ReportProtocolServerRpc(int protocol, int passive)
        {
            if (!IsServer) return;
            var p = (BoarderProtocolId)protocol;
            var a = (BoarderAbilityId)passive;
            if (!BoarderProtocol.IsLegalPair(p, a))
            {
                p = BoarderProtocolId.Default;
                a = BoarderAbilityId.Ghost;
            }
            ProtocolId.Value = (int)p;
            PassiveId.Value = (int)a;
            if (_health != null)
            {
                float mul = a == BoarderAbilityId.Ghost
                    ? BoarderProtocol.Magnitude(BoarderAbilityId.Ghost) : 1f;
                _health.ConfigureAcquisition(mul);
            }
        }
    }
}
