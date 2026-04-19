using System.Collections.Generic;
using UnityEngine;
using UnityServiceLocator;

namespace Gameplay.Audio
{
    /// <summary>
    /// Pooled AudioSource service. One-shots and loops route through here so mixer
    /// assignment, 2D/3D spatialization, and priority-based voice stealing are
    /// centralized. Resolved via ServiceLocator.Global.
    /// </summary>
    public class AudioService : MonoBehaviour
    {
        [SerializeField, Min(1), Tooltip("Total pooled AudioSources. ~24 is plenty for a 2D roguelike; bump if rapid fire or explosions cut off important cues.")]
        private int m_poolSize = 24;

        [Header("3D Defaults")]
        [Tooltip("Min distance at which 3D sources reach full volume.")]
        [SerializeField] private float m_default3DMinDistance = 2f;
        [Tooltip("Max distance at which 3D sources become inaudible.")]
        [SerializeField] private float m_default3DMaxDistance = 20f;
        [Tooltip("Rolloff curve between min and max distance for 3D sources.")]
        [SerializeField] private AnimationCurve m_default3DRolloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        private readonly List<PooledSource> m_pool = new List<PooledSource>();
        private int m_nextLoopToken = 1;

        private void Awake()
        {
            ServiceLocator.Global.Register(this);

            for (int i = 0; i < m_poolSize; i++)
            {
                GameObject host = new GameObject($"PooledSource_{i:D2}");
                host.transform.SetParent(transform, false);
                AudioSource src = host.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.rolloffMode = AudioRolloffMode.Custom;
                src.SetCustomCurve(AudioSourceCurveType.CustomRolloff, m_default3DRolloff);
                src.minDistance = m_default3DMinDistance;
                src.maxDistance = m_default3DMaxDistance;
                m_pool.Add(new PooledSource(src));
            }
        }

        public void Play(SoundEvent ev) => PlayInternal(ev, Vector3.zero, null, false);
        public void PlayAt(SoundEvent ev, Vector3 position) => PlayInternal(ev, position, null, false);
        public void PlayAttached(SoundEvent ev, Transform follow) => PlayInternal(ev, Vector3.zero, follow, false);

        public LoopHandle StartLoop(SoundEvent ev, Transform follow = null)
        {
            return PlayInternal(ev, follow != null ? follow.position : Vector3.zero, follow, true);
        }

        public void StopLoop(LoopHandle handle)
        {
            if (!handle.IsValid) return;

            for (int i = 0; i < m_pool.Count; i++)
            {
                if (m_pool[i].LoopToken == handle.Token)
                {
                    m_pool[i].Stop();
                    return;
                }
            }
        }

        private LoopHandle PlayInternal(SoundEvent ev, Vector3 position, Transform follow, bool loop)
        {
            if (ev == null || ev.Resource == null) return default;

            PooledSource source = Acquire(ev.Priority);
            if (source == null) return default;

            int loopToken = loop ? m_nextLoopToken++ : 0;
            source.Configure(ev, position, follow, loopToken);
            source.Play();
            return loop ? new LoopHandle(loopToken) : default;
        }

        private PooledSource Acquire(int incomingPriority)
        {
            PooledSource weakest = null;

            for (int i = 0; i < m_pool.Count; i++)
            {
                PooledSource p = m_pool[i];

                if (!p.IsPlaying)
                    return p;

                if (p.LoopToken == 0
                    && p.Priority <= incomingPriority
                    && (weakest == null || p.Priority < weakest.Priority))
                {
                    weakest = p;
                }
            }

            return weakest;
        }

        private void LateUpdate()
        {
            for (int i = 0; i < m_pool.Count; i++)
                m_pool[i].UpdateFollow();
        }
    }

    internal class PooledSource
    {
        private readonly AudioSource m_source;
        private Transform m_follow;

        public int Priority { get; private set; }
        public int LoopToken { get; private set; }
        public bool IsPlaying => m_source.isPlaying;

        public PooledSource(AudioSource source)
        {
            m_source = source;
        }

        public void Configure(SoundEvent ev, Vector3 position, Transform follow, int loopToken)
        {
            m_follow = follow;
            Priority = ev.Priority;
            LoopToken = loopToken;

            m_source.resource = ev.Resource;
            m_source.outputAudioMixerGroup = ev.MixerGroup;
            m_source.loop = ev.IsLoop;
            m_source.volume = ev.VolumeScale;
            m_source.spatialBlend = ev.Is3D ? 1f : 0f;
            // Unity's engine-level priority: 0 = highest, 256 = lowest. Map our
            // 0–100 game priority onto that range inverted so higher game priority
            // also wins Unity's own voice arbitration when channels are exhausted.
            m_source.priority = Mathf.Clamp(256 - ev.Priority * 2, 0, 256);

            m_source.transform.position = follow != null ? follow.position : position;
        }

        public void Play()
        {
            m_source.Play();
        }

        public void Stop()
        {
            m_source.Stop();
            LoopToken = 0;
            m_follow = null;
        }

        public void UpdateFollow()
        {
            if (m_follow != null && m_source.isPlaying)
                m_source.transform.position = m_follow.position;
        }
    }

    public readonly struct LoopHandle
    {
        public readonly int Token;
        internal LoopHandle(int token) { Token = token; }
        public bool IsValid => Token != 0;
    }
}
