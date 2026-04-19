using UnityEngine;
using UnityEngine.Audio;

namespace Gameplay.Audio
{
    /// <summary>
    /// Ducks SFX/Music while a dialogue line is on screen by transitioning between
    /// two AudioMixer snapshots. Drop once in the scene with a reference to the
    /// DialogueBoxUI that drives line visibility, plus the two snapshots authored
    /// on the mixer (Default and DialogueActive).
    /// </summary>
    public class DialogueDuck : MonoBehaviour
    {
        [SerializeField] private DialogueBoxUI m_dialogueBox;

        [Tooltip("Snapshot applied when no dialogue line is visible.")]
        [SerializeField] private AudioMixerSnapshot m_defaultSnapshot;

        [Tooltip("Snapshot applied while a dialogue line is visible (SFX/Music ducked).")]
        [SerializeField] private AudioMixerSnapshot m_dialogueSnapshot;

        [Tooltip("Seconds the mixer takes to transition between snapshots.")]
        [SerializeField] private float m_transitionDuration = 0.25f;

        private bool m_wasVisible;

        private void Start()
        {
            if (m_defaultSnapshot != null)
                m_defaultSnapshot.TransitionTo(0f);
        }

        private void Update()
        {
            if (m_dialogueBox == null) return;

            bool visible = m_dialogueBox.IsLineVisible;
            if (visible == m_wasVisible) return;

            m_wasVisible = visible;
            AudioMixerSnapshot target = visible ? m_dialogueSnapshot : m_defaultSnapshot;
            if (target != null)
                target.TransitionTo(m_transitionDuration);
        }
    }
}
