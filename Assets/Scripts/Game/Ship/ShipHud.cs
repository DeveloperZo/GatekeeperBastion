using System.Collections.Generic;
using GatekeeperBastion.Sim;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: the Ship hub's HUD — uGUI via <see cref="ShipHudView"/>.
    /// Crosshair, the Interactor's prompt, the shortcut panel, salvage header and objective line.</summary>
    public sealed class ShipHud : MonoBehaviour
    {
        const float RebindInterval = 0.5f;

        ShipController _ship;
        Interactor _interactor;
        FirstPersonController _fpc;
        Transform _camera;
        TextMesh[] _labels;
        bool _labelsScanned;
        float _rebindTimer;
        ShipHudView _view;
        readonly List<string> _shortcutLines = new List<string>(16);

        void Awake()
        {
            _ship = GetComponent<ShipController>();
            if (_ship == null) _ship = FindFirstObjectByType<ShipController>();
        }

        void Start()
        {
            Bind();
            _view = GetComponent<ShipHudView>();
            if (_view == null) _view = GetComponentInChildren<ShipHudView>(true);
            _view?.EnsureBuilt();
        }

        void LateUpdate()
        {
            _rebindTimer -= Time.deltaTime;
            if (_rebindTimer <= 0f && (_interactor == null || _camera == null || _fpc == null))
            {
                _rebindTimer = RebindInterval;
                Bind();
            }
            OptionsScreen.Tick(_fpc);
            BillboardLabels();
            PushHud();
        }

        void PushHud()
        {
            if (_view == null) return;
            var m = GameSession.Meta;
            if (m == null)
            {
                _view.SetVisible(false);
                return;
            }

            bool hide = OptionsScreen.IsOpen || (_ship != null && _ship.IsPanelOpen);
            _view.SetVisible(!hide);
            if (hide) return;

            _view.SetHeader(
                "ABOARD YOUR SHIP",
                $"Banked salvage: {m.BankedSalvage}      Loadout: {LoadoutLabel(m)}",
                Objective(m),
                "ARMORY: west   |   FABRICATION: east   |   NAV MAP: north   |   CREW QUARTERS: south");

            _view.SetCrosshairVisible(true);

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                _view.SetCursorFreeWarn(true);
            }
            else
            {
                string prompt = _interactor != null && !string.IsNullOrEmpty(_interactor.CurrentPrompt)
                    ? _interactor.CurrentPrompt
                    : "";
                _view.SetPrompt(prompt);
            }

            Controls.TickPanelToggle();
            bool collapsed = Controls.Collapsed;
            if (collapsed)
            {
                ShortcutPanel.EstimateCollapsedHeight();
                _view.SetShortcutLines(_shortcutLines, true);
            }
            else
            {
                ShortcutPanel.FillLines(ControlContext.Ship, _shortcutLines);
                _view.SetShortcutLines(_shortcutLines, false);
            }
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

        static string LoadoutLabel(MetaState m)
        {
            if (!m.OwnsAnyWeapon) return "NONE";
            string s = m.EquippedSecondary != WeaponId.None
                ? WeaponCatalog.Def(m.EquippedSecondary).displayName
                : "—";
            string p = m.EquippedPrimary != WeaponId.None
                ? WeaponCatalog.Def(m.EquippedPrimary).displayName
                : "—";
            return $"P:{p} S:{s}";
        }
    }
}
