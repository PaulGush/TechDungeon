using System.Collections;
using UnityEngine;
using UnityServiceLocator;

namespace Gameplay.Audio
{
    /// <summary>
    /// Drives background music track selection and cross-fades between tracks in
    /// response to RoomManager room loads and BossPhaseManager phase changes.
    /// Owns two dedicated AudioSources so rapid-fire SFX voice stealing never
    /// touches the music layer.
    /// </summary>
    public class MusicDirector : MonoBehaviour
    {
        [Header("Tracks")]
        [SerializeField] private SoundEvent m_exploreTrack;
        [SerializeField] private SoundEvent m_combatTrack;
        [Tooltip("Optional shop-specific track. Falls back to the explore track when empty.")]
        [SerializeField] private SoundEvent m_shopTrack;
        [Tooltip("One entry per boss phase. When BossPhaseManager.OnPhaseChanged fires with index N, this array's N-th track plays. Falls back to the last entry if there are more phases than tracks.")]
        [SerializeField] private SoundEvent[] m_bossPhaseTracks;

        [Header("Fade")]
        [Tooltip("Cross-fade duration in seconds between tracks.")]
        [SerializeField] private float m_fadeDuration = 1.5f;

        private AudioSource m_sourceA;
        private AudioSource m_sourceB;
        private AudioSource m_active;
        private AudioSource m_inactive;
        private SoundEvent m_currentTrack;
        private Coroutine m_fadeCoroutine;

        private RoomManager m_roomManager;
        private BossPhaseManager m_bossPhaseManager;

        private void Awake()
        {
            ServiceLocator.Global.Register(this);

            m_sourceA = CreateSource("MusicA");
            m_sourceB = CreateSource("MusicB");
            m_active = m_sourceA;
            m_inactive = m_sourceB;
        }

        private AudioSource CreateSource(string sourceName)
        {
            GameObject host = new GameObject(sourceName);
            host.transform.SetParent(transform, false);
            AudioSource src = host.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            src.volume = 0f;
            src.spatialBlend = 0f;
            return src;
        }

        private void Start()
        {
            if (!ServiceLocator.Global.TryGet(out m_roomManager)) return;
            m_roomManager.OnRoomLoaded += HandleRoomLoaded;
        }

        private void OnDestroy()
        {
            if (m_roomManager != null)
                m_roomManager.OnRoomLoaded -= HandleRoomLoaded;
            UnhookBoss();
        }

        private void HandleRoomLoaded(RoomSettings settings)
        {
            UnhookBoss();

            SoundEvent track;
            switch (settings.RoomType)
            {
                case RoomType.Combat:
                    track = m_combatTrack;
                    break;
                case RoomType.Boss:
                    HookBoss();
                    track = ResolveBossPhaseTrack(m_bossPhaseManager != null ? m_bossPhaseManager.CurrentPhaseIndex : 0);
                    break;
                case RoomType.Shop:
                    track = m_shopTrack != null ? m_shopTrack : m_exploreTrack;
                    break;
                default:
                    track = m_exploreTrack;
                    break;
            }

            PlayTrack(track);
        }

        private void HookBoss()
        {
            // BossPhaseManager isn't globally registered — it lives on the boss prefab
            // which the encounter spawns when the boss room loads. FindFirstObjectByType
            // is acceptable here because it runs once per boss room load.
            m_bossPhaseManager = Object.FindFirstObjectByType<BossPhaseManager>();
            if (m_bossPhaseManager != null)
                m_bossPhaseManager.OnPhaseChanged += HandleBossPhaseChanged;
        }

        private void UnhookBoss()
        {
            if (m_bossPhaseManager == null) return;
            m_bossPhaseManager.OnPhaseChanged -= HandleBossPhaseChanged;
            m_bossPhaseManager = null;
        }

        private void HandleBossPhaseChanged(int phaseIndex)
        {
            PlayTrack(ResolveBossPhaseTrack(phaseIndex));
        }

        private SoundEvent ResolveBossPhaseTrack(int phaseIndex)
        {
            if (m_bossPhaseTracks == null || m_bossPhaseTracks.Length == 0) return null;
            int clamped = Mathf.Clamp(phaseIndex, 0, m_bossPhaseTracks.Length - 1);
            return m_bossPhaseTracks[clamped];
        }

        public void PlayTrack(SoundEvent track)
        {
            if (track == m_currentTrack) return;
            m_currentTrack = track;

            if (m_fadeCoroutine != null)
                StopCoroutine(m_fadeCoroutine);
            m_fadeCoroutine = StartCoroutine(FadeTo(track));
        }

        private IEnumerator FadeTo(SoundEvent track)
        {
            if (track == null || track.Resource == null)
            {
                yield return FadeOut(m_active);
                m_fadeCoroutine = null;
                yield break;
            }

            m_inactive.resource = track.Resource;
            m_inactive.outputAudioMixerGroup = track.MixerGroup;
            m_inactive.volume = 0f;
            m_inactive.Play();

            float targetVolume = track.VolumeScale;
            float startActiveVolume = m_active.volume;
            float elapsed = 0f;

            while (elapsed < m_fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / m_fadeDuration);
                m_active.volume = Mathf.Lerp(startActiveVolume, 0f, t);
                m_inactive.volume = Mathf.Lerp(0f, targetVolume, t);
                yield return null;
            }

            m_active.Stop();
            m_active.volume = 0f;

            AudioSource swap = m_active;
            m_active = m_inactive;
            m_inactive = swap;
            m_fadeCoroutine = null;
        }

        private IEnumerator FadeOut(AudioSource source)
        {
            float startVolume = source.volume;
            float elapsed = 0f;
            while (elapsed < m_fadeDuration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, elapsed / m_fadeDuration);
                yield return null;
            }
            source.Stop();
            source.volume = 0f;
        }
    }
}
