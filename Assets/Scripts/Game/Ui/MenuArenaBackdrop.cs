using UnityEngine;
using UnityEngine.UI;

namespace GatekeeperBastion.Game.Ui
{
    /// <summary>Restrained animated scrim behind the main menu when Bastion cannot load additive.
    /// Blocker: Bastion's Awake boots GameController / WaveSpawner / player — not a silent arena.</summary>
    public sealed class MenuArenaBackdrop : MonoBehaviour
    {
        [SerializeField] RectTransform _drift;
        [SerializeField] float _orbitSeconds = 48f;
        [SerializeField] float _driftPixels = 12f;

        Vector2 _origin;

        void Awake()
        {
            if (_drift == null) _drift = transform as RectTransform;
            if (_drift != null) _origin = _drift.anchoredPosition;
        }

        void Update()
        {
            if (_drift == null || _orbitSeconds <= 0f) return;
            float t = Time.unscaledTime / _orbitSeconds * Mathf.PI * 2f;
            _drift.anchoredPosition = _origin + new Vector2(Mathf.Cos(t), Mathf.Sin(t * 0.7f)) * _driftPixels;
        }

        /// <summary>No-op: colour comes from Shift UI Manager on the authored Image.</summary>
        public static void ApplyRestrainedLook(Image image)
        {
            if (image == null) return;
            image.raycastTarget = false;
        }
    }
}
