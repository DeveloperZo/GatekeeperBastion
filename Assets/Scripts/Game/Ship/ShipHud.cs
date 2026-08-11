using System.Text;
using GatekeeperBastion.Game.Ui;
using GatekeeperBastion.Sim;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: the Ship hub's HUD — uGUI via <see cref="ShipHudView"/>.
    /// Bastion corner cards (Profile / Score / Weapon; no Vitals), crosshair, and interact
    /// prompt when looking at a console. No slogans, objectives, or shortcut strip on the
    /// corners — those belong in station panels. Visibility is state-change only;
    /// steady-state LateUpdate writes values only (M35 26-27).</summary>
    public sealed class ShipHud : MonoBehaviour
    {
        const float RebindInterval = 0.5f;

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
        int _cachedRuns = int.MinValue;
        int _cachedDeepest = int.MinValue;
        string _cachedPrompt = "\0";
        bool _cachedCursorFree;

        readonly StringBuilder _lineBuf = new StringBuilder(160);

        void Awake()
        {
            _ship = GetComponent<ShipController>();
            if (_ship == null) _ship = FindFirstObjectByType<ShipController>();
            UiCanvasGuard.RepairIfBroken(gameObject);
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                canvas.transform.localScale = Vector3.one;
        }

        void Start()
        {
            Bind();
            if (_view == null) _view = GetComponent<ShipHudView>();
            if (_view == null) _view = GetComponentInChildren<ShipHudView>(true);
            ApplyRelevance(true);
            if (_view != null)
            {
                _view.EnsureBuilt();
                // Same Profile slots as Bastion until a real level/XP model exists.
                _view.SetProfile("LV 1", "EXP —");
                _view.SetScore("", "");
                _view.SetWeapon("—", "—");
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
            int deepest = m.DeepestWaveReached;
            int runs = m.TotalRuns;
            if (_cachedSalvage != m.BankedSalvage || _cachedLoadoutHash != loadoutHash
                || _cachedRuns != runs || _cachedDeepest != deepest)
            {
                _cachedSalvage = m.BankedSalvage;
                _cachedLoadoutHash = loadoutHash;
                _cachedRuns = runs;
                _cachedDeepest = deepest;

                _view.SetProfile("LV 1", "EXP —");

                _lineBuf.Clear();
                _lineBuf.Append("Salvage ").Append(m.BankedSalvage);
                string salvage = _lineBuf.ToString();
                _lineBuf.Clear();
                if (deepest < 1) deepest = 1;
                _lineBuf.Append("Wave ").Append(deepest)
                    .Append("  ·  ").Append(runs).Append(" runs");
                _view.SetScore(salvage, _lineBuf.ToString());

                AppendLoadout(_view, m);
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
                // E opens Armory from anywhere; console raycast prompts are optional overlays.
                string prompt = _interactor != null && !string.IsNullOrEmpty(_interactor.CurrentPrompt)
                    ? _interactor.CurrentPrompt
                    : "[" + Controls.DisplayOf(ControlId.Interact) + "] ARMORY";
                if (prompt != _cachedPrompt)
                {
                    _cachedPrompt = prompt;
                    _view.SetPrompt(prompt);
                }
            }
        }

        void ApplyRelevance(bool relevant)
        {
            if (relevant == _hudRelevant) return;
            _hudRelevant = relevant;
            if (_view != null) _view.SetHudRelevant(relevant);
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

        static void AppendLoadout(ShipHudView view, MetaState m)
        {
            if (!m.OwnsAnyWeapon)
            {
                view.SetWeapon("—", "—");
                return;
            }
            string primary = m.EquippedPrimary != WeaponId.None
                ? WeaponCatalog.Def(m.EquippedPrimary).displayName
                : "—";
            string secondary = m.EquippedSecondary != WeaponId.None
                ? WeaponCatalog.Def(m.EquippedSecondary).displayName
                : "—";
            view.SetWeapon(primary, secondary);
        }

        static int LoadoutHash(MetaState m) =>
            ((int)m.EquippedPrimary * 397) ^ (int)m.EquippedSecondary;
    }
}
