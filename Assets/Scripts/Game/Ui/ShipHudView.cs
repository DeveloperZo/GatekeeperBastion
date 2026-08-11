using GatekeeperBastion.Game.Ui;
using TMPro;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>Binds the Ship hub's always-on HUD: Bastion corner cards minus Vitals
    /// (Profile TL, Score TR, Weapon BR), plus crosshair and interact prompt. Controllers
    /// push strings; nothing here sets a colour, a font size or a RectTransform
    /// (M35 criteria 25, 38).</summary>
    public sealed class ShipHudView : MonoBehaviour
    {
        [SerializeField] RectTransform _root;

        [Header("Corner cards")]
        [SerializeField] GameObject _profileRoot;
        [SerializeField] TMP_Text _profileLevel;
        [SerializeField] TMP_Text _profileExp;
        [SerializeField] GameObject _scoreRoot;
        [SerializeField] TMP_Text _scoreSalvage;
        [SerializeField] TMP_Text _scoreMeta;
        [SerializeField] GameObject _weaponCardRoot;
        [SerializeField] TMP_Text _weaponName;
        [SerializeField] TMP_Text _weaponAmmo;
        [SerializeField, OptionalBinding] GameObject _reloadBlock;

        [Header("Crosshair / Prompt")]
        [SerializeField] GameObject _crosshair;
        [SerializeField] TextMeshProUGUI _prompt;

        bool _promptIsWarn;
        bool _missingLogged;

        void Awake()
        {
            UiCanvasGuard.RepairIfBroken(gameObject);
            EnsureBuilt();
        }

        void OnEnable() => UiCanvasGuard.RepairIfBroken(gameObject);

        public void EnsureBuilt()
        {
            if (_missingLogged) return;
            _missingLogged = true;
            LogMissing(nameof(_root), _root);
            LogMissing(nameof(_profileRoot), _profileRoot);
            LogMissing(nameof(_scoreRoot), _scoreRoot);
            LogMissing(nameof(_weaponCardRoot), _weaponCardRoot);
            // Ship does not use a clip/reload bar; keep the Weapon card shell only.
            UiBind.Show(_reloadBlock, false);
        }

        void LogMissing(string field, Object value)
        {
            if (value == null)
                Debug.LogError($"[ShipHudView] '{name}' missing serialized {field}.", this);
        }

        public void SetProfile(string levelLine, string expLine)
        {
            UiBind.SetText(_profileLevel, levelLine);
            UiBind.SetText(_profileExp, expLine ?? "");
        }

        public void SetScore(string salvageLine, string metaLine)
        {
            UiBind.SetText(_scoreSalvage, salvageLine);
            UiBind.SetText(_scoreMeta, metaLine ?? "");
        }

        public void SetWeapon(string name, string ammoLine)
        {
            UiBind.SetText(_weaponName, name ?? "");
            UiBind.SetText(_weaponAmmo, ammoLine ?? "");
        }

        public void SetPrompt(string text)
        {
            _promptIsWarn = false;
            UiBind.SetTextOrHide(_prompt, text);
        }

        public void SetCursorFreeWarn(bool show)
        {
            if (!show)
            {
                if (_promptIsWarn) UiBind.SetTextOrHide(_prompt, null);
                _promptIsWarn = false;
                return;
            }
            _promptIsWarn = true;
            UiBind.SetTextOrHide(_prompt, "<b>CURSOR FREE</b> - left-click to take back mouse look");
        }

        public void SetCrosshairVisible(bool visible) => UiBind.Show(_crosshair, visible);

        /// <summary>State-change only — callers must not toggle every frame (M35 criterion 26).</summary>
        public void SetHudRelevant(bool relevant)
        {
            UiBind.Show(_root, relevant);
            UiBind.Show(_profileRoot, relevant);
            UiBind.Show(_scoreRoot, relevant);
            UiBind.Show(_weaponCardRoot, relevant);
        }

        public void SetVisible(bool visible) => SetHudRelevant(visible);
    }
}
