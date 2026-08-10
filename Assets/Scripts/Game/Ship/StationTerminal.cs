using System.Collections;
using GatekeeperBastion.Sim;
using GatekeeperBastion.Util;
using Michsky.UI.Shift;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: base class for ship-hub console interactables — Interact opens the
    /// station panel (canvas activated, <see cref="MainPanelManager.OpenPanel"/>), close button
    /// or Esc via <see cref="ShipController"/> exits. Esc is never polled here (M35 Escape
    /// contract / criterion 21).</summary>
    public abstract class StationTerminal : MonoBehaviour, IInteractable
    {
        const string Tag = "StationTerminal";
        const float ReopenLockoutSeconds = 0.3f;
        const float PanelOutCapSeconds = 0.3f;
        const string PanelOutState = "Panel Out";

        [SerializeField] StationPanelView _view;
        [SerializeField] GameObject _canvasRoot;
        [SerializeField] MainPanelManager _panels;

        ShipController _ship;
        float _reopenAllowedAt;
        int _lastContentHash;
        bool _wasOpen;
        bool _closing;
        bool _missingLogged;

        protected abstract string StationName { get; }
        protected abstract string PromptHint { get; }

        public bool IsOpen => _ship != null && _ship.OpenStation == this;

        public string Prompt =>
            "[" + Controls.DisplayOf(ControlId.Interact) + "] " + StationName + " — " + PromptHint;

        public void Interact(float dt)
        {
            if (IsOpen || _closing || Time.time < _reopenAllowedAt) return;
            if (Ship != null) Ship.OpenStationPanel(this);
        }

        ShipController Ship => _ship != null ? _ship : (_ship = FindFirstObjectByType<ShipController>());

        protected virtual void Awake()
        {
            _ship = FindFirstObjectByType<ShipController>();
            ValidateBindings();
        }

        void ValidateBindings()
        {
            if (_view != null && _canvasRoot != null && _panels != null) return;
            if (_missingLogged) return;
            _missingLogged = true;
            if (_view == null)
                Log.Error(Tag, StationName + ": missing serialized _view (M35 criterion 24).");
            if (_canvasRoot == null)
                Log.Error(Tag, StationName + ": missing serialized _canvasRoot (M35 criterion 24).");
            if (_panels == null)
                Log.Error(Tag, StationName + ": missing serialized _panels (M35 criterion 24).");
        }

        /// <summary>Called by <see cref="ShipController"/> after it records this as the open
        /// station — activates the canvas and opens the Shift panel.</summary>
        public void OnOpenedByShip()
        {
            if (_canvasRoot != null && !_canvasRoot.activeSelf)
                _canvasRoot.SetActive(true);
            OpenShiftPanel();
            _wasOpen = false;
            _lastContentHash = 0;
            _closing = false;
        }

        void LateUpdate()
        {
            if (!IsOpen || _closing)
            {
                if (_wasOpen && !IsOpen)
                {
                    _wasOpen = false;
                    _lastContentHash = 0;
                }
                return;
            }

            var meta = GameSession.Meta;
            if (meta == null || OptionsScreen.IsOpen)
                return;

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
            if (_view == null)
            {
                ValidateBindings();
                return;
            }

            _view.SetOnClose(Close);
            _view.SetCloseLabel("CLOSE  (" + Controls.DisplayOf(ControlId.FreeCursor) + ")");
            _view.SetHeader(
                StationName,
                meta.BankedSalvage,
                "Lifetime: " + meta.LifetimeSalvageEarned + " earned  —  " + meta.LifetimeSalvageSpent + " spent");
            _view.ClearBody();
            BuildPanel(_view, meta);
            _view.PlayEntrance();
            OpenShiftPanel();
        }

        void OpenShiftPanel()
        {
            if (_panels == null) return;
            _panels.OpenPanel(StationName);
        }

        protected abstract void BuildPanel(StationPanelView view, MetaState m);

        /// <summary>Close via button or Esc dispatcher: panel-out, wait, deactivate canvas,
        /// restore walk (M35 criterion 21).</summary>
        public void Close()
        {
            if (_closing) return;
            if (!IsOpen && !_wasOpen) return;
            _reopenAllowedAt = Time.time + ReopenLockoutSeconds;
            StartCoroutine(CloseRoutine());
        }

        IEnumerator CloseRoutine()
        {
            _closing = true;
            Animator anim = null;
            if (_view != null) anim = _view.GetComponent<Animator>();
            if (anim != null && anim.isActiveAndEnabled)
                anim.Play(PanelOutState);

            float elapsed = 0f;
            while (elapsed < PanelOutCapSeconds)
            {
                if (anim != null && IsPanelOutDone(anim)) break;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (_canvasRoot != null && _canvasRoot.activeSelf)
                _canvasRoot.SetActive(false);

            _wasOpen = false;
            _lastContentHash = 0;
            _closing = false;
            if (Ship != null) Ship.CloseStationPanel(this);
        }

        static bool IsPanelOutDone(Animator anim)
        {
            var info = anim.GetCurrentAnimatorStateInfo(0);
            return info.IsName(PanelOutState) && info.normalizedTime >= 1f;
        }

        protected static void UpgradeRow(StationPanelView view, UpgradeTrack track, string trackName,
            System.Func<int, string> effect, string lockedLabel = null)
        {
            var meta = GameSession.Meta;
            int level = ShipCommands.LevelOf(track);
            bool maxed = level >= MetaUpgrades.MaxLevel;
            string now = effect(level);
            string effectLine = maxed ? now : now + " → " + effect(level + 1);
            string levelLine = level + "/" + MetaUpgrades.MaxLevel;

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
            string reason = canBuy ? null : "need " + (cost - banked) + " more";
            view.AddPricedRow(
                trackName,
                "cost " + cost,
                levelLine,
                effectLine,
                "BUY",
                () => ShipCommands.BuyUpgrade(track),
                canBuy,
                reason);
        }
    }
}

// wp3-refresh
