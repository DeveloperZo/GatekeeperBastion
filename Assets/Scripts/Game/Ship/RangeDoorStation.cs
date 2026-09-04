using System.Collections.Generic;
using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: the way into the practice range, beside the ARMORY. A station
    /// rather than a slab that loads on a hold, because nothing may load until a kit has been
    /// chosen and confirmed: the panel offers a protocol and two weapons, an owned weapon at
    /// the realm's own wheel points and an unowned one at base, and CONFIRM hands that kit to
    /// <see cref="GameSession.PendingRangeLoadout"/> before <see cref="SceneFlow.LoadRange"/>.
    /// The live-Run refusal stays in the loader. CANCEL closes the panel and loads nothing.
    ///
    /// Five authored rows, bound the way the ARMORY binds its catalog: three selectors that
    /// cycle on their button, the confirm row, the cancel row.</summary>
    public sealed class RangeDoorStation : StationTerminal
    {
        const string Tag = "Range";
        const int ProtocolRow = 0;
        const int PrimaryRow = 1;
        const int SecondaryRow = 2;
        const int ConfirmRow = 3;
        const int CancelRow = 4;
        public const int RowCount = 5;

        [SerializeField, Tooltip("Five rows: Protocol, Primary, Secondary, CONFIRM, CANCEL.")]
        PanelRowView[] _rows;

        public override string StationName => "PRACTICE RANGE";
        public const string PurposeLine = "a kit, tried before it is paid for";
        public override string Purpose => PurposeLine;
        public const string ConfirmLine = "ENTER THE RANGE — practice, nothing is saved";
        public const string CancelLine = "Back to the ship";

        protected override string PromptHint => "hold — kit up for the practice range";

        BoarderProtocolId[] _protocols;
        WeaponId[] _primaries;
        WeaponId[] _secondaries;
        int _protocolIndex;
        int _primaryIndex;
        int _secondaryIndex;
        bool _loading;
        bool _loadFailed;

        protected override void Awake()
        {
            base.Awake();
            _protocols = BoarderProtocol.ShopProtocols();
            _primaries = ForSlot(WeaponSlot.Primary);
            _secondaries = ForSlot(WeaponSlot.Secondary);
            if (_rows == null || _rows.Length < RowCount)
                Log.Error(Tag, StationName + ": needs " + RowCount + " serialized _rows.");
        }

        static WeaponId[] ForSlot(WeaponSlot slot)
        {
            var list = new List<WeaponId>(4);
            foreach (var id in WeaponCatalog.ForSlot(slot)) list.Add(id);
            return list.ToArray();
        }

        /// <summary>Open on what the Boarder carries, so CONFIRM with nothing touched is the
        /// kit in hand; anything not carried falls to the first row of its list.</summary>
        protected override void OnStationOpened(MetaState m)
        {
            _loading = false;
            _loadFailed = false;
            _protocolIndex = IndexOf(_protocols, m.EquippedProtocol);
            _primaryIndex = IndexOf(_primaries, m.EquippedPrimary);
            _secondaryIndex = IndexOf(_secondaries, m.EquippedSecondary);
        }

        static int IndexOf<T>(T[] list, T value) where T : struct
        {
            for (int i = 0; i < list.Length; i++)
                if (list[i].Equals(value)) return i;
            return 0;
        }

        protected override int ContentHash(MetaState m)
        {
            unchecked
            {
                int h = base.ContentHash(m);
                h = h * 397 ^ _protocolIndex;
                h = h * 397 ^ _primaryIndex;
                h = h * 397 ^ _secondaryIndex;
                h = h * 397 ^ (_loading ? 1 : 0);
                h = h * 397 ^ (_loadFailed ? 1 : 0);
                return h;
            }
        }

        protected override void BuildPanel(StationPanelView view, MetaState m)
        {
            if (_rows == null || _rows.Length < RowCount) return;

            BoarderProtocolId protocol = _protocols[_protocolIndex];
            WeaponId primary = _primaries[_primaryIndex];
            WeaponId secondary = _secondaries[_secondaryIndex];

            BindSelector(_rows[ProtocolRow], "Protocol", BoarderProtocol.ProtocolName(protocol),
                () => _protocolIndex = (_protocolIndex + 1) % _protocols.Length);
            BindSelector(_rows[PrimaryRow], "Primary", WeaponLabel(m, primary),
                () => _primaryIndex = (_primaryIndex + 1) % _primaries.Length);
            BindSelector(_rows[SecondaryRow], "Secondary", WeaponLabel(m, secondary),
                () => _secondaryIndex = (_secondaryIndex + 1) % _secondaries.Length);

            _rows[ConfirmRow].Bind(ConfirmLine, "", "", "", "CONFIRM",
                () => Confirm(m, protocol, primary, secondary), !_loading,
                _loadFailed ? "range refused — see the log" : null);
            UiBind.Show(_rows[ConfirmRow], true);

            _rows[CancelRow].Bind(CancelLine, "", "", "", "CANCEL", () => Close(), true);
            UiBind.Show(_rows[CancelRow], true);

            for (int r = RowCount; r < _rows.Length; r++)
                UiBind.Show(_rows[r], false);
        }

        static void BindSelector(PanelRowView row, string name, string value, System.Action onNext)
        {
            if (row == null) return;
            row.Bind(name, "", "", value, "NEXT", onNext, true);
            UiBind.Show(row, true);
        }

        /// <summary>"&lt;name&gt; — T&lt;n&gt;" for an owned weapon, "&lt;name&gt; — base (not
        /// owned)" otherwise: the same rule <see cref="RangeLoadout.From"/> applies.</summary>
        public static string WeaponLabel(MetaState m, WeaponId id)
        {
            string name = WeaponCatalog.Def(id).displayName;
            return m.Owns(id)
                ? name + " — T" + m.TierOf(id)
                : name + " — base (not owned)";
        }

        void Confirm(MetaState m, BoarderProtocolId protocol, WeaponId primary, WeaponId secondary)
        {
            if (_loading) return;
            RangeLoadout kit = RangeLoadout.From(m, protocol, primary, secondary);
            GameSession.PendingRangeLoadout = kit;
            Log.Event(Tag, "range kit chosen at the door — " + kit.Summary());
            _loading = SceneFlow.LoadRange();
            _loadFailed = !_loading;
            if (_loadFailed)
            {
                // The loader has already said why. Nothing is pending for a range that did
                // not load, or the next direct open would adopt a kit nobody asked for.
                GameSession.PendingRangeLoadout = null;
            }
        }
    }
}
