using UnityEngine;
using UnityEngine.Audio;

namespace Gameplay.Audio
{
    [CreateAssetMenu(menuName = "Data/Audio/Sound Event", fileName = "SoundEvent_")]
    public class SoundEvent : ScriptableObject
    {
        [Tooltip("AudioClip or AudioRandomContainer. Prefer an ARC for any event with variation — pitch/volume randomization and avoid-repeat live on the ARC asset, not here.")]
        [SerializeField] private AudioResource m_resource;

        [Tooltip("Mixer group this sound routes through (Music / SFX / UI / Dialogue).")]
        [SerializeField] private AudioMixerGroup m_mixerGroup;

        [Tooltip("True for world-space positional sounds (e.g., enemy fire, chest open). False for 2D UI/player cues that shouldn't attenuate with distance.")]
        [SerializeField] private bool m_is3D;

        [Tooltip("True for looped sounds (e.g., flamethrower loop, drone hover). Loops return a LoopHandle from AudioService so they can be stopped later.")]
        [SerializeField] private bool m_isLoop;

        [Tooltip("0–100. Higher priority wins voice stealing when the pool is saturated. Use the tiered constants in AudioPriorities.")]
        [SerializeField, Range(0, 100)] private int m_priority = 50;

        [Tooltip("Volume multiplier stacked on top of the ARC's own volume randomization. Usually leave at 1 and tune on the ARC.")]
        [SerializeField, Range(0f, 1f)] private float m_volumeScale = 1f;

        public AudioResource Resource => m_resource;
        public AudioMixerGroup MixerGroup => m_mixerGroup;
        public bool Is3D => m_is3D;
        public bool IsLoop => m_isLoop;
        public int Priority => m_priority;
        public float VolumeScale => m_volumeScale;
    }
}
