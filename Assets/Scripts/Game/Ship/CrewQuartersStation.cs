using System;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    public sealed class CrewQuartersStation : StationTerminal
    {
        const string Tag = "CrewQuartersStation";

        public const string StubVeil = "VEIL";
        public const string StubAegis = "AEGIS";
        public const string StubLedger = "LEDGER";
        public const string ComingSoon = "Coming Soon";
        public const string StubSheet = "Coming Soon — no sheet yet";
        public const string SelectLabel = "SELECT";

        [SerializeField] CrewQuartersPanelView _body;

        bool _showingSheet;
        int _selected;

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

        protected override void OnStationOpened(MetaState meta)
        {
            _showingSheet = false;
            _selected = 0;
        }

        public override bool TryBack()
        {
            if (!_showingSheet) return false;
            _showingSheet = false;
            return true;
        }

        protected override int ContentHash(MetaState m)
        {
            unchecked
            {
                return base.ContentHash(m)
                    ^ (int)m.EquippedProtocol * 17
                    ^ (int)m.EquippedPassive * 31
                    ^ _selected * 13
                    ^ (_showingSheet ? 1 : 0);
            }
        }

        protected override void BuildPanel(StationPanelView view, MetaState m)
        {
            if (_body == null) return;
            if (_showingSheet)
                BuildSheetMode(view, m);
            else
                BuildCatalogMode(view, m);
        }

        void BuildCatalogMode(StationPanelView view, MetaState m)
        {
            view.SetOnClose(() => Close());
            view.SetCloseLabel("CLOSE  (" + Controls.DisplayOf(ControlId.FreeCursor) + ")");
            _body.ShowSheet(false);
            _body.SetCatalogVisible(true);
            var ids = BoarderProtocol.ShopProtocols();
            var id = ids.Length > 0 ? ids[0] : BoarderProtocolId.Default;
            bool owned = m.EquippedProtocol == id;
            BindCatalogRow(_body.ProtocolRow, BoarderProtocol.ProtocolName(id), 0, owned);
            BindCatalogRow(_body.GhostRow, StubVeil, 1, false);
            BindCatalogRow(_body.ForesightRow, StubAegis, 2, false);
            BindCatalogRow(_body.UnusedRow, StubLedger, 3, false);
        }

        void BuildSheetMode(StationPanelView view, MetaState m)
        {
            _body.SetCatalogVisible(false);
            _body.ShowSheet(true);
            view.SetOnClose(() => { _showingSheet = false; });
            view.SetCloseLabel(ProtocolSheetView.BackLabel);

            var ids = BoarderProtocol.ShopProtocols();
            var id = ids.Length > 0 ? ids[0] : BoarderProtocolId.Default;
            bool owned = m.EquippedProtocol == id;
            BoarderProtocol.Sheet sheet;
            string claim;
            bool canBuy;
            Action onClaim;
            if (_selected == 0)
            {
                sheet = BoarderProtocol.SheetOf(id);
                canBuy = !owned && !m.PayloadLoadoutLocked;
                claim = owned ? "EQUIPPED" : "BUY";
                onClaim = canBuy ? () => ShipCommands.BuyProtocol(id) : null;
            }
            else
            {
                string stub = _selected == 1 ? StubVeil : _selected == 2 ? StubAegis : StubLedger;
                sheet = new BoarderProtocol.Sheet(stub, "", StubSheet, StubSheet, StubSheet);
                claim = ComingSoon;
                canBuy = false;
                onClaim = null;
            }

            if (_body.Sheet != null)
                _body.Sheet.Bind(sheet, claim, onClaim, canBuy, () => { _showingSheet = false; });
        }

        void BindCatalogRow(PanelRowView row, string name, int index, bool owned)
        {
            if (row == null) return;
            int captured = index;
            row.gameObject.SetActive(true);
            row.Bind(
                name,
                "",
                owned ? "EQUIPPED" : "",
                "",
                SelectLabel,
                () => { _selected = captured; _showingSheet = true; },
                true);
        }
    }
}
