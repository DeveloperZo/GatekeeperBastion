using UnityEngine;
using UnityEngine.EventSystems;

namespace GatekeeperBastion.Game.Ui
{
    /// <summary>Sets UI Audio pitch to ±5% before Shift <c>UIElementSound</c> plays.</summary>
    public sealed class UiPitchRandomizer : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        const float Jitter = 0.05f;

        public void OnPointerEnter(PointerEventData eventData) => JitterPitch();

        public void OnPointerClick(PointerEventData eventData) => JitterPitch();

        static void JitterPitch()
        {
            var go = GameObject.Find("UI Audio");
            if (go == null) return;
            var src = go.GetComponent<AudioSource>();
            if (src == null) return;
            src.pitch = Random.Range(1f - Jitter, 1f + Jitter);
        }
    }
}
