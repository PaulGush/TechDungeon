using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityServiceLocator;

public class CinematicPlayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayableDirector m_director;
    [SerializeField] private DialogueBoxUI m_dialogueBox;
    [SerializeField] private SkipPromptUI m_skipPrompt;

    [Header("Skip")]
    [SerializeField] private float m_skipHoldDuration = 3f;

    private bool m_advanceRequested;
    private bool m_isPlaying;
    private InputAction m_advanceAction;
    private InputAction m_skipAction;
    private float m_skipHoldTime;
    private bool m_skipTriggered;

    public bool IsPlaying => m_isPlaying;
    public event Action OnCinematicComplete;

    private void Awake()
    {
        ServiceLocator.Global.Register(this);

        // Run the timeline in unscaled time so death cinematics (which slow Time.timeScale)
        // still play at real speed and stay in sync with the dialogue typing — DialogueBoxUI
        // already drives its typewriter via WaitForSecondsRealtime.
        m_director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;

        m_advanceAction = new InputAction("CinematicAdvance", InputActionType.Button);
        m_advanceAction.AddBinding("<Keyboard>/anyKey");
        m_advanceAction.AddBinding("<Mouse>/leftButton");
        m_advanceAction.AddBinding("<Gamepad>/buttonSouth");
        m_advanceAction.AddBinding("<Gamepad>/buttonEast");

        m_skipAction = new InputAction("CinematicSkip", InputActionType.Button);
        m_skipAction.AddBinding("<Keyboard>/space");

        m_director.stopped += OnDirectorStopped;
    }

    private void OnDestroy()
    {
        m_director.stopped -= OnDirectorStopped;
        m_advanceAction?.Dispose();
        m_skipAction?.Dispose();
    }

    private void Start()
    {
        // Catch PlayOnAwake — the director may already be playing before we could subscribe to played
        if (m_director.state == PlayState.Playing)
        {
            BeginCinematic();
        }
    }

    private void BeginCinematic()
    {
        m_isPlaying = true;
        m_advanceRequested = false;
        m_skipHoldTime = 0f;
        m_skipTriggered = false;

        if (m_skipPrompt != null)
            m_skipPrompt.Hide();

        if (m_director.playableAsset is TimelineAsset timeline)
        {
            BindDialogueTrack(timeline);
        }

        m_advanceAction.performed += OnAdvance;
        m_advanceAction.Enable();
        m_skipAction.Enable();
    }

    private void OnDirectorStopped(PlayableDirector director)
    {
        m_advanceAction.performed -= OnAdvance;
        m_advanceAction.Disable();
        m_skipAction.Disable();

        m_skipHoldTime = 0f;
        m_skipTriggered = false;
        if (m_skipPrompt != null)
            m_skipPrompt.Hide();

        if (m_dialogueBox != null)
            m_dialogueBox.Hide();

        m_isPlaying = false;
        OnCinematicComplete?.Invoke();
    }

    private void Update()
    {
        if (!m_isPlaying) return;

        TickSkipHold();
        if (m_skipTriggered) return;

        if (!m_advanceRequested || !IsPausedForInput()) return;

        m_advanceRequested = false;

        if (m_dialogueBox != null && m_dialogueBox.IsTyping)
        {
            m_dialogueBox.CompleteLine();
        }
        else
        {
            ResumeTimeline();
        }
    }

    private void TickSkipHold()
    {
        if (m_skipTriggered) return;

        if (m_skipAction.IsPressed())
        {
            m_skipHoldTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(m_skipHoldTime / m_skipHoldDuration);

            if (m_skipPrompt != null)
                m_skipPrompt.SetProgress(progress);

            if (m_skipHoldTime >= m_skipHoldDuration)
            {
                m_skipTriggered = true;
                if (m_skipPrompt != null)
                    m_skipPrompt.Hide();
                // Resume first so the timeline can actually reach its end and fire `stopped`.
                ResumeTimeline();
                m_director.Stop();
            }
        }
        else if (m_skipHoldTime > 0f)
        {
            m_skipHoldTime = 0f;
            if (m_skipPrompt != null)
                m_skipPrompt.Hide();
        }
    }

    public Coroutine Play(TimelineAsset timeline)
    {
        return StartCoroutine(PlayCinematic(timeline));
    }

    private IEnumerator PlayCinematic(TimelineAsset timeline)
    {
        m_director.playableAsset = timeline;
        m_director.Play();
        BeginCinematic();

        // Skip a frame so the initial button press that entered the room doesn't immediately advance
        yield return null;
        m_advanceRequested = false;

        while (m_isPlaying)
        {
            yield return null;
        }
    }

    private void BindDialogueTrack(TimelineAsset timeline)
    {
        foreach (var output in timeline.outputs)
        {
            if (output.sourceObject is DialogueTrack)
            {
                m_director.SetGenericBinding(output.sourceObject, m_dialogueBox);
            }
        }
    }

    /// <summary>
    /// Resolves the <see cref="ExposedReference{T}"/> on every <see cref="CinemachineShot"/>
    /// clip in the timeline to <paramref name="vcam"/>. Required for runtime-swapped
    /// timelines because <c>PlayableDirector.playableAsset =</c> doesn't carry exposed
    /// references — without this call, CinemachineShots resolve to null and the brain
    /// has no driving vcam, so the camera renders from the bare Camera transform (0,0,0
    /// in our scene). Call before <see cref="Play"/>.
    /// </summary>
    public void BindAllCinemachineShots(TimelineAsset timeline, CinemachineCamera vcam)
    {
        if (timeline == null || vcam == null) return;

        foreach (TrackAsset track in timeline.GetOutputTracks())
        {
            if (track is not CinemachineTrack cmTrack) continue;

            foreach (TimelineClip clip in cmTrack.GetClips())
            {
                if (clip.asset is CinemachineShot shot)
                    m_director.SetReferenceValue(shot.VirtualCamera.exposedName, vcam);
            }
        }
    }

    private bool IsPausedForInput()
    {
        if (!m_director.playableGraph.IsValid()) return false;
        var root = m_director.playableGraph.GetRootPlayable(0);
        return root.IsValid() && root.GetSpeed() == 0;
    }

    private void ResumeTimeline()
    {
        if (!m_director.playableGraph.IsValid()) return;
        var root = m_director.playableGraph.GetRootPlayable(0);
        if (root.IsValid())
        {
            root.SetSpeed(1);
        }
    }

    private void OnAdvance(InputAction.CallbackContext context)
    {
        if (m_dialogueBox == null || !m_dialogueBox.IsLineVisible) return;
        m_advanceRequested = true;
    }
}
