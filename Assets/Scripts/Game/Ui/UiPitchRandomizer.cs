using UnityEngine;
using UnityEngine.EventSystems;

namespace GatekeeperBastion.Game
{
    /// <summary>LOCAL-OWNER: sets UI Audio pitch to ±5% before Shift <c>UIElementSound</c> plays.</summary>
    public sealed class UiPitchRandomizer : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        const float Jitter = 0.05f;

        [SerializeField, Tooltip("The 'UI Audio' AudioSource Shift plays through. Authored in the scene.")]
        AudioSource _uiAudio;

        public void OnPointerEnter(PointerEventData eventData) => JitterPitch();

        public void OnPointerClick(PointerEventData eventData) => JitterPitch();

        void JitterPitch()
        {
            if (_uiAudio == null) return;
            _uiAudio.pitch = Random.Range(1f - Jitter, 1f + Jitter);
        }
    }
}
