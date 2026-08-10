using GatekeeperBastion.Sim;
using Michsky.UI.Shift;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: base class for ship-hub console interactables — press E to open a
    /// Shift panel via <see cref="MainPanelManager.OpenPanel"/> (cursor freed, movement locked
    /// via ShipController), Esc/CLOSE exits.</summary>
    public abstract class StationTerminal : MonoBehaviour, IInteractable
    {
        const float ReopenLockoutSeconds = 0.3f;

        ShipController _ship;
        float _reopenAllowedAt;
        StationPanelView _view;
        MainPanelManager _panels;
        int _lastContentHash;
        bool _wasOpen;

        protected abstract string StationName { get; }
        protected abstract string PromptHint { get; }

        public bool IsOpen => _ship != null && _ship.OpenStation == this;

        public string Prompt => $"[{Controls.DisplayOf(ControlId.Interact)}] {StationName} — {PromptHint}";

        public void Interact(float dt)
        {
            if (IsOpen || Time.time < _reopenAllowedAt) return;
            if (Ship != null) Ship.OpenStationPanel(this);
        }

        ShipController Ship => _ship != null ? _ship : (_ship = FindFirstObjectByType<ShipController>());

        protected virtual void Awake()
        {
            _ship = FindFirstObjectByType<ShipController>();
        }

        void LateUpdate()
        {
            if (IsOpen && Controls.Down(ControlId.FreeCursor)) Close();
            if (!IsOpen)
            {
                if (_wasOpen)
                {
                    _view?.SetVisible(false);
                    _wasOpen = false;
                    _lastContentHash = 0;
                }
                return;
            }

            var meta = GameSession.Meta;
            if (meta == null || OptionsScreen.IsOpen)
            {
                _view?.SetVisible(false);
                return;
            }

            // Wheel rotate must be polled every frame even when the body is not rebuilt.
            TickWhileOpen(meta);

            int hash = ContentHash(meta);
            bool force = !_wasOpen;
            if (force || hash != _lastContentHash)
            {
                RefreshPanel(meta);
                _lastContentHash = hash;
            }
            else
            {
                _view?.SetVisible(true);
                OpenShiftPanel();
            }
            _wasOpen = true;
        }

        /// <summary>Per-frame input while open (e.g. ARMORY wheel rotate). Default: none.</summary>
        protected virtual void TickWhileOpen(MetaState meta) { }

        /// <summary>Hash of panel-driving state. Subclasses that rebuild on input should
        /// include those inputs (see ArmoryStation).</summary>
        protected virtual int ContentHash(MetaState meta)
        {
            unchecked
            {
                return meta.BankedSalvage.GetHashCode() * 397
                    ^ (int)meta.LifetimeSalvageEarned
                    ^ (int)meta.LifetimeSalvageSpent
                    ^ meta.DeepestWaveReached;
            }
        }

        void RefreshPanel(MetaState meta)
        {
            _view = StationPanelView.EnsureInstance();
            if (_view != null)
            {
                _view.SetOnClose(Close);
                _view.SetCloseLabel($"CLOSE  ({Controls.DisplayOf(ControlId.FreeCursor)})");
                _view.SetHeader(
                    StationName,
                    meta.BankedSalvage,
                    $"Lifetime: {meta.LifetimeSalvageEarned} earned  —  {meta.LifetimeSalvageSpent} spent");
                _view.ClearBody();
                BuildPanel(_view, meta);
                _view.PlayEntrance();
                _view.SetVisible(true);
            }
            OpenShiftPanel();
        }

        void OpenShiftPanel()
        {
            if (_panels == null)
                _panels = FindFirstObjectByType<MainPanelManager>(FindObjectsInactive.Include);
            if (_panels == null) return;
            _panels.OpenPanel(StationName);
        }

        protected abstract void BuildPanel(StationPanelView view, MetaState m);

        protected void Close()
        {
            _reopenAllowedAt = Time.time + ReopenLockoutSeconds;
            _view?.SetVisible(false);
            if (Ship != null) Ship.CloseStationPanel(this);
        }

        protected static void UpgradeRow(StationPanelView view, UpgradeTrack track, string trackName,
            System.Func<int, string> effect, string lockedLabel = null)
        {
            var meta = GameSession.Meta;
            int level = ShipCommands.LevelOf(track);
            bool maxed = level >= MetaUpgrades.MaxLevel;
            string now = effect(level);
            string effectLine = maxed ? now : $"{now} → {effect(level + 1)}";
            string levelLine = $"{level}/{MetaUpgrades.MaxLevel}";

            if (lockedLabel != null)
            {
                view.AddPricedRow(trackName, "", levelLine, effectLine, "BUY", null, false, lockedLabel);
                return;
            }

            if (maxed)
            {
                view.AddPricedRow(trackName, "", levelLine, effectLine, "BUY", null, false,
                    "MAX — fully upgraded");
                return;
            }

            long cost = MetaUpgrades.NextCost(level);
            long banked = meta != null ? meta.BankedSalvage : 0L;
            bool canBuy = banked >= cost;
            string reason = canBuy ? null : $"need {cost - banked} more";
            view.AddPricedRow(
                trackName,
                $"cost {cost}",
                levelLine,
                effectLine,
                "BUY",
                () => ShipCommands.BuyUpgrade(track),
                canBuy,
                reason);
        }
    }
}
