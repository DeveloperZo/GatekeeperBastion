using TMPro;
using UnityEngine;

namespace GatekeepersProtocol.Game
{
    /// <summary>LOCAL-OWNER: the Ship HUD has no vitals card, so the demo's wake fight gets a
    /// minimal HP readout — visible while intruders are aboard or the player is hurt during
    /// the demo, hidden otherwise. One authored TMP line (scratch scene pass); values only,
    /// state-change writes.</summary>
    public sealed class DemoShipVitals : MonoBehaviour
    {
        [SerializeField, OptionalBinding, Tooltip("HP readout line, bottom-left of the Ship HUD canvas.")]
        TMP_Text _line;

        int _shownHp = int.MinValue;
        bool _shownVisible;

        void LateUpdate()
        {
            if (_line == null) return;

            var m = GameSession.Meta;
            var player = PlayerRegistry.Local;
            bool demo = m != null && m.DemoActive;
            bool hurt = player != null && player.Hp < player.MaxHp - 0.5f;
            bool visible = demo && player != null && (DemoShipFight.IsActive || hurt);

            if (visible != _shownVisible)
            {
                _shownVisible = visible;
                _line.gameObject.SetActive(visible);
                if (!visible) _shownHp = int.MinValue;
            }
            if (!visible) return;

            int hp = Mathf.CeilToInt(player.Hp);
            if (hp == _shownHp) return;
            _shownHp = hp;
            _line.text = "HP " + hp + "/" + Mathf.CeilToInt(player.MaxHp);
        }
    }
}
