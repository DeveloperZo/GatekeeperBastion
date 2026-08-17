using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using Unity.Netcode;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER (partial of NetPlayerAvatar): protocol/passive equip push.
    /// Split so the avatar file stays under cap.</summary>
    public sealed partial class NetPlayerAvatar
    {
        public override void OnGainedOwnership()
        {
            base.OnGainedOwnership();
            if (!IsSpawned || !IsOwner) return;
            BindOwnerRig();
            ReportOwnerMeta();
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
