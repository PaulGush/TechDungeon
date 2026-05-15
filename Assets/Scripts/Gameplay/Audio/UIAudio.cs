using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityServiceLocator;

namespace Gameplay.Audio
{
    /// <summary>
    /// Drops onto a UI Button. Plays 2D UI SoundEvents on hover and click without
    /// each button author having to wire onClick manually. Leave either SoundEvent
    /// empty to skip that cue.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class UIAudio : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, ISelectHandler, ISubmitHandler
    {
        [SerializeField] private SoundEvent m_hoverSound;
        [SerializeField] private SoundEvent m_clickSound;

        private AudioService m_audio;

        public void OnPointerEnter(PointerEventData eventData) => Play(m_hoverSound);
        public void OnPointerClick(PointerEventData eventData) => Play(m_clickSound);
        public void OnSelect(BaseEventData eventData) => Play(m_hoverSound);
        public void OnSubmit(BaseEventData eventData) => Play(m_clickSound);

        private void Play(SoundEvent ev)
        {
            if (ev == null) return;
            if (m_audio == null && !ServiceLocator.Global.TryGet(out m_audio))
                return;
            m_audio.Play(ev);
        }
    }
}
