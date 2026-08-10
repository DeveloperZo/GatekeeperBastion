using System.Collections.Generic;
using System.Text;
using GatekeeperBastion.Sim;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: the Ship hub's HUD — uGUI via <see cref="ShipHudView"/>.
    /// Crosshair, the Interactor's prompt, the shortcut panel, salvage header and objective line.
    /// Visibility is state-change only; steady-state LateUpdate writes values only (M35 26-27).</summary>
    public sealed class ShipHud : MonoBehaviour
    {
        const float RebindInterval = 0.5f;
        const string TitleText = "ABOARD YOUR SHIP";
        const string DirectoryText =
            "ARMORY: west   |   FABRICATION: east   |   NAV MAP: north   |   CREW QUARTERS: south";

        [SerializeField] ShipHudView _view;

        ShipController _ship;
        Interactor _interactor;
        FirstPersonController _fpc;
        Transform _camera;
        TextMesh[] _labels;
        bool _labelsScanned;
        float _rebindTimer;
        bool _hudRelevant = true;
        long _cachedSalvage = long.MinValue;
        int _cachedLoadoutHash = int.MinValue;
        int _cachedObjectiveHash = int.MinValue;
        string _cachedPrompt = "\0";
        bool _cachedCursorFree;
        bool _cachedCollapsed;
        int _cachedShortcutHash = int.MinValue;

        readonly List<ShortcutRow> _shortcutRows = new List<ShortcutRow>(16);
        readonly StringBuilder _salvageBuf = new StringBuilder(96);

        void Awake()
        {
            _ship = GetComponent<ShipController>();
            if (_ship == null) _ship = FindFirstObjectByType<ShipController>();
        }

        void Start()
        {
            Bind();
            if (_view == null) _view = GetComponent<ShipHudView>();
            if (_view == null) _view = GetComponentInChildren<ShipHudView>(true);
            ApplyRelevance(true);
            if (_view != null)
            {
                _view.SetHeader(TitleText, "", "", DirectoryText);
                _view.SetCrosshairVisible(true);
            }
        }

        void LateUpdate()
        {
            _rebindTimer -= Time.deltaTime;
            if (_rebindTimer <= 0f && (_interactor == null || _camera == null || _fpc == null))
            {
                _rebindTimer = RebindInterval;
                Bind();
            }
            OptionsScreen.TickDisplayRevert();
            BillboardLabels();
            PushHud();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnGUI()
        {
            float scale = Screen.height / 1080f;
            if (scale < 0.01f) scale = 1f;
            var prev = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            DevTaintBanner.Draw(1920f);
            GUI.matrix = prev;
        }
#endif

        void PushHud()
        {
            if (_view == null) return;
            var m = GameSession.Meta;
            bool hide = m == null || OptionsScreen.IsOpen
                || (_ship != null && (_ship.IsPanelOpen || _ship.IsSettingsOpen));
            ApplyRelevance(!hide);
            if (hide || m == null) return;

            int loadoutHash = LoadoutHash(m);
            if (_cachedSalvage != m.BankedSalvage || _cachedLoadoutHash != loadoutHash
                || _cachedObjectiveHash != ObjectiveHash(m))
            {
                _cachedSalvage = m.BankedSalvage;
                _cachedLoadoutHash = loadoutHash;
                _cachedObjectiveHash = ObjectiveHash(m);
                _salvageBuf.Clear();
                _salvageBuf.Append("Banked salvage: ").Append(m.BankedSalvage)
                    .Append("      Loadout: ").Append(LoadoutLabel(m));
                _view.SetHeader(TitleText, _salvageBuf.ToString(), Objective(m), DirectoryText);
            }

            bool cursorFree = Cursor.lockState != CursorLockMode.Locked;
            if (cursorFree != _cachedCursorFree)
            {
                _cachedCursorFree = cursorFree;
                _view.SetCursorFreeWarn(cursorFree);
                if (cursorFree) _cachedPrompt = "\0";
            }

            if (!cursorFree)
            {
                string prompt = _interactor != null && !string.IsNullOrEmpty(_interactor.CurrentPrompt)
                    ? _interactor.CurrentPrompt
                    : "";
                if (prompt != _cachedPrompt)
                {
                    _cachedPrompt = prompt;
                    _view.SetPrompt(prompt);
                }
            }

            Controls.TickPanelToggle();
            bool collapsed = Controls.Collapsed;
            if (collapsed)
            {
                if (!_cachedCollapsed)
                {
                    _cachedCollapsed = true;
                    _cachedShortcutHash = int.MinValue;
                    _view.SetShortcutRows(_shortcutRows, true);
                }
                return;
            }

            ShortcutPanel.FillRows(ControlContext.Ship, _shortcutRows);
            int sh = ShortcutHash(_shortcutRows);
            if (_cachedCollapsed || sh != _cachedShortcutHash)
            {
                _cachedCollapsed = false;
                _cachedShortcutHash = sh;
                _view.SetShortcutRows(_shortcutRows, false);
            }
        }

        void ApplyRelevance(bool relevant)
        {
            if (relevant == _hudRelevant) return;
            _hudRelevant = relevant;
            _view?.SetHudRelevant(relevant);
        }

        void Bind()
        {
            if (_ship == null) _ship = FindFirstObjectByType<ShipController>();

            var player = PlayerRegistry.Local;
            if (player != null)
            {
                _interactor = player.GetComponentInChildren<Interactor>();
                _fpc = player.GetComponent<FirstPersonController>();
            }
            if (_interactor == null) _interactor = FindFirstObjectByType<Interactor>();
            if (_fpc == null) _fpc = FindFirstObjectByType<FirstPersonController>();

            var cam = Camera.main;
            _camera = cam != null ? cam.transform : null;

            if (_labelsScanned) return;
            _labelsScanned = true;
            _labels = FindObjectsByType<TextMesh>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        }

        void BillboardLabels()
        {
            if (_labels == null || _camera == null) return;
            Vector3 eye = _camera.position;
            for (int i = 0; i < _labels.Length; i++)
            {
                var label = _labels[i];
                if (label == null) continue;
                Vector3 away = label.transform.position - eye;
                away.y = 0f;
                if (away.sqrMagnitude < 0.0001f) continue;
                label.transform.rotation = Quaternion.LookRotation(away, Vector3.up);
            }
        }

        static string Objective(MetaState m)
        {
            return m.OwnsAnyWeapon
                ? "OBJECTIVE: EMBARK at the NAV MAP — or buy attributes at CREW QUARTERS."
                : "OBJECTIVE: free MK-1 at ARMORY, attributes at CREW QUARTERS, EMBARK at NAV MAP.";
        }

        static int ObjectiveHash(MetaState m) => m.OwnsAnyWeapon ? 1 : 0;

        static int LoadoutHash(MetaState m) =>
            ((int)m.EquippedPrimary * 397) ^ (int)m.EquippedSecondary;

        static string LoadoutLabel(MetaState m)
        {
            if (!m.OwnsAnyWeapon) return "NONE";
            string s = m.EquippedSecondary != WeaponId.None
                ? WeaponCatalog.Def(m.EquippedSecondary).displayName
                : "—";
            string p = m.EquippedPrimary != WeaponId.None
                ? WeaponCatalog.Def(m.EquippedPrimary).displayName
                : "—";
            return "P:" + p + " S:" + s;
        }

        static int ShortcutHash(IList<ShortcutRow> rows)
        {
            unchecked
            {
                int h = rows.Count;
                for (int i = 0; i < rows.Count; i++)
                {
                    h = h * 397 ^ (rows[i].Key != null ? rows[i].Key.GetHashCode() : 0);
                    h = h * 397 ^ (rows[i].Description != null ? rows[i].Description.GetHashCode() : 0);
                }
                return h;
            }
        }
    }
}

// wp3-refresh
