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
        public const string EquipLabel = "EQUIP";
        public const string EquippedLabel = "EQUIPPED";

        /// <summary>Deck copy. It says free and it says reversible on purpose: a first-time
        /// player shown three unfamiliar sheets will stall on a choice they think is
        /// permanent, and at this station it is neither.</summary>
        public const string PickHeading =
            "CHOOSE YOUR PROTOCOL — costs nothing, and you can swap it here between Boardings";

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

        // With nothing equipped the catalog's job is not to be browsed, it is to be chosen
        // from once, so the panel opens on the deck instead. _browsing is the way back to the
        // list (the Coming Soon rows are only reachable there) and resets on every open, so
        // the deck is what an unequipped Boarder meets each visit without the deck ever being
        // a wall — CLOSE works from it exactly as it does from the catalog.
        bool _browsing;
        readonly BoarderProtocolId[] _offer = new BoarderProtocolId[ProtocolOffer.CardCount];

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
            _browsing = false;
            if (meta == null || !meta.DemoActive) return;
            // The menu unlocking folds the beat 0 heal in — CREW keeps restoring health
            // on a visit for the rest of the demo (beat 4, Traps).
            DemoStationGate.HealOnVisit(meta);
            if (!_claimLinePlayed && meta.EquippedProtocol == BoarderProtocolId.None)
            {
                _claimLinePlayed = true;
                DemoVoice.Play(DemoConversationIds.CrewClaim, DemoVoiceMode.Subtitle);
            }
        }

        public override bool TryBack()
        {
            if (_showingSheet)
            {
                _showingSheet = false;
                return true;
            }
            // The catalog was reached FROM the deck, so Esc walks back to the deck rather
            // than out of the station — the same one-step-back the sheet already gets.
            if (_browsing && DeckAvailable(GameSession.Meta))
            {
                _browsing = false;
                return true;
            }
            return false;
        }

        /// <summary>True when this scene has an authored deck and the save is in a state
        /// where a pick can actually be committed. Both halves are required: a deck the
        /// scene does not have cannot be shown, and a deck whose cards would all refuse is
        /// a worse read than the catalog, which at least labels what it is showing.</summary>
        bool DeckAvailable(MetaState m)
        {
            if (_body == null || _body.CardDeck == null) return false;
            return ProtocolOffer.IsPickPending(m);
        }

        protected override int ContentHash(MetaState m)
        {
            unchecked
            {
                return base.ContentHash(m)
                    ^ (int)m.EquippedProtocol * 17
                    ^ (int)m.EquippedPassive * 31
                    ^ _selected * 13
                    ^ (_showingSheet ? 1 : 0)
                    ^ (_browsing ? 1 << 8 : 0);
            }
        }

        protected override void BuildPanel(StationPanelView view, MetaState m)
        {
            if (_body == null) return;
            bool picking = !_browsing && DeckAvailable(m);
            _body.ShowCardDeck(picking);
            if (picking)
            {
                BuildPickMode(view, m);
                return;
            }
            if (_showingSheet)
                BuildSheetMode(view, m);
            else
                BuildCatalogMode(view, m);
        }

        /// <summary>Three cards, dealt by the Sim rule and committed through the same
        /// <see cref="ShipCommands.BuyProtocol"/> the catalog's claim uses — the claim is 0
        /// salvage, so this is a pick and the card says CHOOSE rather than BUY. A card the
        /// command would refuse is painted with the reason instead of being lit and dead.
        /// </summary>
        void BuildPickMode(StationPanelView view, MetaState m)
        {
            var deck = _body.CardDeck;
            if (deck == null) return;
            _body.SetCatalogVisible(false);
            _body.ShowSheet(false);
            view.SetOnClose(() => Close());
            view.SetCloseLabel("CLOSE  (" + Controls.DisplayOf(ControlId.FreeCursor) + ")");
            deck.SetHeading(PickHeading);

            int dealt = ProtocolOffer.Deal(m, _offer);
            for (int i = 0; i < deck.CardCount; i++)
            {
                var card = deck.CardAt(i);
                if (card == null) continue;
                if (i >= dealt)
                {
                    UiBind.Show(card, false);
                    continue;
                }
                var id = _offer[i];
                string refusal = ProtocolOffer.RefuseReason(m, id);
                bool canPick = refusal == null;
                var chosen = id;
                UiBind.Show(card, true);
                card.Bind(
                    BoarderProtocol.SheetOf(id),
                    ProtocolCardView.PickLabel,
                    canPick ? (Action)(() => ShipCommands.BuyProtocol(chosen)) : null,
                    canPick,
                    refusal);
            }

            deck.BindBrowse(ProtocolCardDeckView.BrowseLabel, () => _browsing = true);
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
                claim = owned ? EquippedLabel : EquipLabel;
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

            // Chrome Close is the back control. Bind(onBack: null) hides the in-sheet Back.
            if (_body.Sheet != null)
                _body.Sheet.Bind(sheet, claim, onClaim, canBuy, onBack: null);
        }

        void BindCatalogRow(PanelRowView row, string name, int index, bool owned)
        {
            if (row == null) return;
            int captured = index;
            row.gameObject.SetActive(true);
            row.Bind(
                name,
                "",
                owned ? EquippedLabel : "\u00A0",
                "",
                SelectLabel,
                () => { _selected = captured; _showingSheet = true; },
                true);
        }
    }
}
