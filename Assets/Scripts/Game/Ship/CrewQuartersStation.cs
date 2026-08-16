using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    public sealed class CrewQuartersStation : StationTerminal
    {
        const string Tag = "CrewQuartersStation";

        [SerializeField] CrewQuartersPanelView _body;

        public override string StationName => "CREW QUARTERS";
        public const string PurposeLine = "you";
        public override string Purpose => PurposeLine;

        protected override string PromptHint => "your protocol";

        protected override void Awake()
        {
            base.Awake();
            if (_body == null)
                Log.Error(Tag, StationName + ": missing serialized _body.");
        }

        protected override int ContentHash(MetaState m)
        {
            unchecked
            {
                return base.ContentHash(m)
                    ^ (int)m.EquippedProtocol * 17
                    ^ (int)m.EquippedPassive * 31;
            }
        }

        protected override void BuildPanel(StationPanelView view, MetaState m)
        {
            if (_body == null) return;
            var ids = BoarderProtocol.ShopProtocols();
            var id = ids.Length > 0 ? ids[0] : BoarderProtocolId.Default;
            string crew = BoarderProtocol.DisplayName(BoarderProtocol.CrewAbilityOf(id));
            string personal = BoarderProtocol.DisplayName(BoarderProtocol.PersonalAbilityOf(id));
            _body.ProtocolRow.Bind(
                BoarderProtocol.ProtocolName(id),
                "",
                "EQUIPPED",
                BoarderProtocol.DoctrineText(id) + "  G " + crew + "  H " + personal,
                "",
                null,
                false);

            int n = BoarderProtocol.PassiveCount(id);
            BindPassive(_body.GhostRow, m, id, 0, n > 0);
            BindPassive(_body.ForesightRow, m, id, 1, n > 1);
            if (_body.UnusedRow != null)
                _body.UnusedRow.gameObject.SetActive(false);
        }

        void BindPassive(PanelRowView row, MetaState m, BoarderProtocolId protocol, int index, bool show)
        {
            if (row == null) return;
            row.gameObject.SetActive(show);
            if (!show) return;
            var pid = BoarderProtocol.PassiveAt(protocol, index);
            bool equipped = m.EquippedPassive == pid;
            var capture = pid;
            row.Bind(
                BoarderProtocol.DisplayName(pid),
                "",
                equipped ? "EQUIPPED" : "",
                pid == BoarderAbilityId.Ghost ? "reduced acquisition" : "next Wave kinds",
                equipped ? "EQUIPPED" : "EQUIP",
                () => ShipCommands.PickPassive(capture),
                !equipped && !m.PayloadLoadoutLocked);
        }
    }
}
