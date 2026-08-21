using System;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    public sealed class CrewQuartersStation : StationTerminal
    {
        const string Tag = "CrewQuartersStation";

        public const string StubAegis = "AEGIS";
        public const string StubLedger = "LEDGER";
        public const string ComingSoon = "Coming Soon";
        public const string StubSheet = "Coming Soon — no sheet yet";
        public const string SelectLabel = "SELECT";

        /// <summary>Protocols the roster names but no slice has authored yet. They pad the
        /// catalog after the sold ones; a protocol leaves this list by entering
        /// <see cref="BoarderProtocol.ShopProtocols"/>, never by being edited in two places.</summary>
        static readonly string[] ComingSoonNames = { StubAegis, StubLedger };

        [SerializeField] CrewQuartersPanelView _body;

        bool _showingSheet;
        int _selected;
        // demo.crew_claim plays on the first panel open of a visit while the Default is
        // unclaimed (instance lifetime is the visit; the claim itself retires the line).
        bool _claimLinePlayed;

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
            if (meta == null || !meta.DemoActive) return;
            // The menu unlocking folds the beat 0 heal in — CREW keeps restoring health
            // on a visit for the rest of the demo (beat 4, Traps).
            DemoStationGate.HealOnVisit(meta);
            if (!_claimLinePlayed && meta.EquippedProtocol == BoarderProtocolId.None)
            {
                _claimLinePlayed = true;
                DemoVoice.Play(DemoConversationIds.CrewClaim, DemoVoiceMode.Lock);
            }
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
            for (int i = 0; i < CrewQuartersPanelView.RowCount; i++)
            {
                var row = _body.RowAt(i);
                if (row == null) continue;
                if (i < ids.Length)
                {
                    BindCatalogRow(row, BoarderProtocol.ProtocolName(ids[i]), i,
                        m.EquippedProtocol == ids[i]);
                    continue;
                }
                int stub = i - ids.Length;
                if (stub < ComingSoonNames.Length)
                {
                    BindCatalogRow(row, ComingSoonNames[stub], i, false);
                    continue;
                }
                UiBind.Show(row, false);
            }
        }

        void BuildSheetMode(StationPanelView view, MetaState m)
        {
            _body.SetCatalogVisible(false);
            _body.ShowSheet(true);
            view.SetOnClose(() => { _showingSheet = false; });
            view.SetCloseLabel(ProtocolSheetView.BackLabel);

            var ids = BoarderProtocol.ShopProtocols();
            BoarderProtocol.Sheet sheet;
            string claim;
            bool canBuy;
            Action onClaim;
            if (_selected >= 0 && _selected < ids.Length)
            {
                var id = ids[_selected];
                bool owned = m.EquippedProtocol == id;
                sheet = BoarderProtocol.SheetOf(id);
                canBuy = !owned && !m.PayloadLoadoutLocked;
                claim = owned ? "EQUIPPED" : "BUY";
                var claimed = id;
                onClaim = canBuy ? () => ShipCommands.BuyProtocol(claimed) : null;
            }
            else
            {
                int stub = _selected - ids.Length;
                string name = stub >= 0 && stub < ComingSoonNames.Length
                    ? ComingSoonNames[stub]
                    : ComingSoon;
                sheet = new BoarderProtocol.Sheet(name, "", StubSheet, StubSheet, StubSheet);
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
