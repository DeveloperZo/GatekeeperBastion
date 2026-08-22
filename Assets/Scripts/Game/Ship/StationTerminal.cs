using GatekeepersProtocol.Sim;
using GatekeepersProtocol.Util;
using Michsky.UI.Shift;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: console Interact opens one Main Panels child and holds it until
    /// Esc/Close via <see cref="ShipController"/>. Esc is never polled here.</summary>
    public abstract class StationTerminal : MonoBehaviour, IInteractable
    {
        const string Tag = "StationTerminal";
        const float ReopenLockoutSeconds = 0.3f;
        const string TopPanelName = "Top Panel";
        const string BottomPanelName = "Bottom Panel";

        [SerializeField] StationPanelView _view;
        [SerializeField] GameObject _canvasRoot;
        [SerializeField] MainPanelManager _panels;

        [SerializeField, Tooltip("Ship-hub controller. Authored in the scene.")]
        ShipController _ship;

        float _reopenAllowedAt;
        int _lastContentHash;
        bool _wasOpen;
        bool _missingLogged;
        bool _openLogged;
        Animator[] _panelAnims;
        CanvasGroup[] _panelGroups;
        bool _visibilityResolved;

        public abstract string StationName { get; }
        public abstract string Purpose { get; }
        protected abstract string PromptHint { get; }

        public bool IsOpen => _ship != null && _ship.OpenStation == this;

        public string Prompt
        {
            get
            {
                // Demo gates own the string while a station is dark / grant-only / heal-only.
                string demo = DemoStationGate.PromptOverride(this);
                if (demo != null) return demo;
                // A down-wait refuses every station but the map, and used to do it while still
                // offering "[E] ARMORY — equip and focus-upgrade the weapon in your hands". The
                // player walked up, pressed, and nothing happened, with the reason in a log they
                // cannot see. Say it on the crosshair instead.
                if (NetShipWait.IsActive && !(this is MapStation))
                    return StationName + " — sealed while the Run is live";
                return "[" + Controls.DisplayOf(ControlId.Interact) + "] " + StationName
                    + " — " + PromptHint;
            }
        }

        /// <summary>Stations open via <see cref="ShipController.TryOpenStationFromInteract"/> on
        /// Interact Down — same path as the working Armory open. The held path forwards to the
        /// demo gate (the dark NAV hold-repair); it no-ops everywhere else.</summary>
        public void Interact(float dt) => DemoStationGate.TickHold(this, dt);

        ShipController Ship => _ship;

        protected virtual void Awake()
        {
            ValidateBindings();
        }

        void ValidateBindings()
        {
            if (_view != null && _canvasRoot != null && _panels != null && _ship != null) return;
            if (_missingLogged) return;
            _missingLogged = true;
            if (_view == null)
                Log.Error(Tag, StationName + ": missing serialized _view.");
            if (_canvasRoot == null)
                Log.Error(Tag, StationName + ": missing serialized _canvasRoot.");
            if (_panels == null)
                Log.Error(Tag, StationName + ": missing serialized _panels.");
            if (_ship == null)
                Log.Error(Tag, StationName + ": missing serialized _ship.");
        }

        /// <summary>Canvas on, this panel on, every other Main Panels child off (Top Panel stays).
        /// MainPanelManager is disabled for the duration so it cannot hide the panel again.</summary>
        public void OnOpenedByShip()
        {
            if (_canvasRoot != null)
            {
                if (!_canvasRoot.activeSelf)
                    _canvasRoot.SetActive(true);
                // Shift ships Canvas at localScale 0 — that paints nothing. Always force 1.
                _canvasRoot.transform.localScale = Vector3.one;
                UiCanvasGuard.RepairIfBroken(_canvasRoot);
                _canvasRoot.transform.localScale = Vector3.one;
                var canvas = _canvasRoot.GetComponent<Canvas>();
                if (canvas != null)
                {
                    if (canvas.sortingOrder < 100)
                        canvas.sortingOrder = 100;
                    canvas.enabled = true;
                }
                // Main Menu chrome: blue Background under Canvas (sibling of Main Panels).
                var bg = _canvasRoot.transform.Find("Background");
                if (bg != null && !bg.gameObject.activeSelf)
                    bg.gameObject.SetActive(true);
            }

            // MPM's DisablePreviousPanel / Panel Out Helper fight a plain SetActive show.
            if (_panels != null)
                _panels.enabled = false;

            if (_view != null)
            {
                _view.gameObject.SetActive(true);
                ShowPanelImmediate(_view.gameObject);
            }

            IsolateAmongMainPanels();
            ForcePanelSubtreeVisible();
            _wasOpen = false;
            _lastContentHash = 0;
            _openLogged = false;

            var meta = GameSession.Meta;
            if (meta != null && !OptionsScreen.IsOpen)
            {
                OnStationOpened(meta);
                RefreshPanel(meta);
                _lastContentHash = ContentHash(meta);
                _wasOpen = true;
            }
        }

        void IsolateAmongMainPanels()
        {
            if (_view == null) return;
            Transform parent = _view.transform.parent;
            if (parent == null) return;
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i).gameObject;
                if (child == _view.gameObject) continue;
                if (child.name == TopPanelName || child.name == BottomPanelName) continue;
                if (child.activeSelf)
                    child.SetActive(false);
            }
            if (!_view.gameObject.activeSelf)
                _view.gameObject.SetActive(true);
            ShowPanelImmediate(_view.gameObject);
            HideSettingsChrome();
            ForcePanelSubtreeVisible();
        }

        /// <summary>Settings-shell leftovers on the station canvas. Esc Settings may keep
        /// its own copy on a different canvas.</summary>
        void HideSettingsChrome()
        {
            HideNamed(_canvasRoot != null ? _canvasRoot.transform : null, "Categories");
            HideNamed(_canvasRoot != null ? _canvasRoot.transform : null, "Preview Image");
        }

        static void HideNamed(Transform root, string name)
        {
            if (root == null) return;
            if (root.name == name)
            {
                if (root.gameObject.activeSelf)
                    root.gameObject.SetActive(false);
                return;
            }
            for (int i = 0; i < root.childCount; i++)
                HideNamed(root.GetChild(i), name);
        }

        static void ShowPanelImmediate(GameObject panel)
        {
            if (panel == null) return;
            var anim = panel.GetComponent<Animator>();
            if (anim != null) anim.enabled = false;
            var group = panel.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }
        }

        /// <summary>Edit mode shows the authored panel; Play samples Shift's Panel Out
        /// Helper on nested Gameplay (and siblings) and writes CanvasGroup.alpha=0 every
        /// frame. Disable every Animator under the station panel and force every
        /// CanvasGroup visible.</summary>
        void ForcePanelSubtreeVisible()
        {
            if (_view == null) return;
            ResolveVisibilityHooks();

            if (_panelAnims != null)
            {
                for (int i = 0; i < _panelAnims.Length; i++)
                {
                    if (_panelAnims[i] != null)
                        _panelAnims[i].enabled = false;
                }
            }

            if (_panelGroups != null)
            {
                for (int i = 0; i < _panelGroups.Length; i++)
                {
                    var g = _panelGroups[i];
                    if (g == null) continue;
                    g.alpha = 1f;
                    g.interactable = true;
                    g.blocksRaycasts = true;
                }
            }
        }

        void ResolveVisibilityHooks()
        {
            if (_visibilityResolved || _view == null) return;
            _visibilityResolved = true;
            _panelAnims = _view.GetComponentsInChildren<Animator>(true);
            _panelGroups = _view.GetComponentsInChildren<CanvasGroup>(true);
        }

        void LateUpdate()
        {
            if (!IsOpen)
            {
                if (_wasOpen)
                {
                    _wasOpen = false;
                    _lastContentHash = 0;
                }
                return;
            }

            // Stay visible until Esc — re-assert scale/siblings (Shift Canvas often reverts to 0).
            if (_canvasRoot != null)
                _canvasRoot.transform.localScale = Vector3.one;
            IsolateAmongMainPanels();

            var meta = GameSession.Meta;
            if (meta == null || OptionsScreen.IsOpen)
                return;

            TickWhileOpen(meta);

            int hash = ContentHash(meta);
            if (!_wasOpen || hash != _lastContentHash)
            {
                RefreshPanel(meta);
                _lastContentHash = hash;
            }
            _wasOpen = true;
        }

        /// <summary>Once per open, before the first <see cref="BuildPanel"/>. Override to pick
        /// which tab the station shows from current meta.</summary>
        protected virtual void OnStationOpened(MetaState meta) { }

        protected virtual void TickWhileOpen(MetaState meta) { }

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

            _view.SetOnClose(() => Close());
            _view.SetCloseLabel("CLOSE  (" + Controls.DisplayOf(ControlId.FreeCursor) + ")");
            _view.SetHeader(
                StationName,
                Purpose,
                meta.BankedSalvage,
                "Lifetime: " + meta.LifetimeSalvageEarned + " earned  —  " + meta.LifetimeSalvageSpent + " spent");
            BuildPanel(_view, meta);
            IsolateAmongMainPanels();
            ForcePanelSubtreeVisible();

            if (!_openLogged)
            {
                _openLogged = true;
                int bodyKids = _view.BodyContent != null ? _view.BodyContent.childCount : -1;
                float gAlpha = -1f;
                if (_panelGroups != null)
                {
                    for (int i = 0; i < _panelGroups.Length; i++)
                    {
                        if (_panelGroups[i] == null) continue;
                        if (_panelGroups[i].gameObject.name != "Gameplay") continue;
                        gAlpha = _panelGroups[i].alpha;
                        break;
                    }
                }
                Log.Event(Tag, StationName + ": refreshed — bodyChildren=" + bodyKids +
                    " gameplayAlpha=" + gAlpha);
            }
        }

        protected abstract void BuildPanel(StationPanelView view, MetaState m);

        /// <summary>Esc first chance: nested UI (ARMORY wheel) pops before the station closes.
        /// Return true when Esc was consumed.</summary>
        public virtual bool TryBack() => false;

        /// <summary>Esc or Close — hide panel + canvas, restore walk.</summary>
        public void Close(string reason = null)
        {
            if (!IsOpen && !_wasOpen) return;
            _reopenAllowedAt = Time.time + ReopenLockoutSeconds;

            if (_view != null && _view.gameObject.activeSelf)
                _view.gameObject.SetActive(false);
            if (_canvasRoot != null && _canvasRoot.activeSelf)
                _canvasRoot.SetActive(false);
            if (_panels != null)
                _panels.enabled = true;

            _wasOpen = false;
            _lastContentHash = 0;
            if (Ship != null) Ship.CloseStationPanel(this);
        }

        /// <summary>Binds one authored upgrade-track row. The row stays visible when the
        /// action cannot apply — locked and maxed states show as disabled with a reason.</summary>
        protected static void UpgradeRow(PanelRowView row, UpgradeTrack track, string trackName,
            System.Func<int, string> effect, string lockedLabel = null)
        {
            if (row == null) return;
            var meta = GameSession.Meta;
            int level = ShipCommands.LevelOf(track);
            bool maxed = level >= MetaUpgrades.MaxLevel;
            string now = effect(level);
            string effectLine = maxed ? now : now + " → " + effect(level + 1);
            string levelLine = level + "/" + MetaUpgrades.MaxLevel;

            if (lockedLabel != null)
            {
                row.Bind(trackName, "", levelLine, effectLine, "BUY", null, false, lockedLabel);
                return;
            }

            if (maxed)
            {
                row.Bind(trackName, "", levelLine, effectLine, "BUY", null, false,
                    "MAX — fully upgraded");
                return;
            }

            long cost = MetaUpgrades.NextCost(level);
            long banked = meta != null ? meta.BankedSalvage : 0L;
            bool canBuy = banked >= cost;
            string refuse = canBuy ? null : "need " + (cost - banked) + " more";
            // The hatch belongs to the realm, and ShipCommands.BuyUpgrade refuses a visitor —
            // but only in the log, so on a guest this button lit up, took the press and did
            // nothing. The UI has to ask the same question the command does.
            if (!RealmRecordSource.OwnsThisRealm())
            {
                canBuy = false;
                refuse = "not your realm to upgrade";
            }
            row.Bind(
                trackName,
                "cost " + cost,
                levelLine,
                effectLine,
                "BUY",
                () => ShipCommands.BuyUpgrade(track),
                canBuy,
                refuse);
        }
    }
}
