using System;
using System.Collections;
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

    private bool m_advanceRequested;
    private bool m_isPlaying;
    private InputAction m_advanceAction;

    public bool IsPlaying => m_isPlaying;
    public event Action OnCinematicComplete;

    private void Awake()
    {
        ServiceLocator.Global.Register(this);

        m_advanceAction = new InputAction("CinematicAdvance", InputActionType.Button);
        m_advanceAction.AddBinding("<Keyboard>/anyKey");
        m_advanceAction.AddBinding("<Mouse>/leftButton");
        m_advanceAction.AddBinding("<Gamepad>/buttonSouth");
        m_advanceAction.AddBinding("<Gamepad>/buttonEast");

        m_director.stopped += OnDirectorStopped;
    }

    private void OnDestroy()
    {
        m_director.stopped -= OnDirectorStopped;
        m_advanceAction?.Dispose();
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

        if (m_director.playableAsset is TimelineAsset timeline)
        {
            BindDialogueTrack(timeline);
        }

        m_advanceAction.performed += OnAdvance;
        m_advanceAction.Enable();
    }

    private void OnDirectorStopped(PlayableDirector director)
    {
        m_advanceAction.performed -= OnAdvance;
        m_advanceAction.Disable();

        if (m_dialogueBox != null)
            m_dialogueBox.Hide();

        m_isPlaying = false;
        OnCinematicComplete?.Invoke();
    }

    private void Update()
    {
        if (!m_isPlaying || !m_advanceRequested || !IsPausedForInput()) return;

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
        m_advanceRequested = true;
    }
}
