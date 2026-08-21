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
            int proto = meta != null ? (int)meta.EquippedProtocol : (int)BoarderProtocolId.None;
            int passive = meta != null ? (int)meta.EquippedPassive : (int)BoarderAbilityId.None;
            ReportProtocolServerRpc(proto, passive);
        }

        [Rpc(SendTo.Server)]
        void ReportProtocolServerRpc(int protocol, int passive)
        {
            if (!IsServer) return;
            var p = (BoarderProtocolId)protocol;
            var a = (BoarderAbilityId)passive;
            // One clamp, the catalog's. The hand-listed Default+Ghost/Foresight case this
            // replaced was the general rule spelled out for the only protocol that existed.
            if (!BoarderProtocol.IsCatalogued(p))
            {
                p = BoarderProtocolId.None;
                a = BoarderAbilityId.None;
            }
            else if (!BoarderProtocol.IsLegalPair(p, a))
            {
                a = BoarderAbilityId.None;
            }
            ProtocolId.Value = (int)p;
            PassiveId.Value = (int)a;
            // Ghost is the one passive that biases acquisition; every other equip is unbiased.
            if (_health != null)
                _health.ConfigureAcquisition(a == BoarderAbilityId.Ghost
                    ? BoarderProtocol.Magnitude(BoarderAbilityId.Ghost)
                    : 1f);
        }
    }
}
