using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueBoxUI : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private RectTransform m_portraitSlot;
    [SerializeField] private Image m_portraitImage;

    [Header("Text")]
    [SerializeField] private TMP_Text m_speakerNameText;
    [SerializeField] private TMP_Text m_dialogueText;

    [Header("Indicator")]
    [SerializeField] private GameObject m_continueIndicator;

    private Coroutine m_typingCoroutine;
    private Coroutine m_portraitCoroutine;
    private bool m_isTyping;
    private string m_fullText;

    public bool IsTyping => m_isTyping;

    public void ShowLine(DialogueLine line, float typingSpeed)
    {
        m_speakerNameText.text = line.SpeakerName;
        m_fullText = line.Text;

        // Portrait
        SetPortrait(line.PortraitFrames, line.PortraitFrameRate, line.Side);

        if (m_continueIndicator != null)
            m_continueIndicator.SetActive(false);

        if (m_typingCoroutine != null)
            StopCoroutine(m_typingCoroutine);

        m_typingCoroutine = StartCoroutine(TypeText(typingSpeed));
    }

    private void SetPortrait(Sprite[] frames, float frameRate, DialogueSide side)
    {
        if (m_portraitCoroutine != null)
            StopCoroutine(m_portraitCoroutine);

        bool hasPortrait = frames != null && frames.Length > 0;
        m_portraitSlot.gameObject.SetActive(hasPortrait);

        if (!hasPortrait) return;

        m_portraitImage.sprite = frames[0];

        // Flip based on side
        Vector3 scale = m_portraitSlot.localScale;
        scale.x = side == DialogueSide.Right ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        m_portraitSlot.localScale = scale;

        if (frames.Length > 1)
        {
            m_portraitCoroutine = StartCoroutine(AnimatePortrait(frames, frameRate));
        }
    }

    private IEnumerator AnimatePortrait(Sprite[] frames, float frameRate)
    {
        float interval = 1f / frameRate;
        int index = 0;

        while (true)
        {
            m_portraitImage.sprite = frames[index];
            index = (index + 1) % frames.Length;
            yield return new WaitForSecondsRealtime(interval);
        }
    }

    public void CompleteLine()
    {
        if (!m_isTyping) return;

        if (m_typingCoroutine != null)
            StopCoroutine(m_typingCoroutine);

        m_dialogueText.text = m_fullText;
        m_isTyping = false;

        if (m_continueIndicator != null)
            m_continueIndicator.SetActive(true);
    }

    private IEnumerator TypeText(float charsPerSecond)
    {
        m_isTyping = true;
        m_dialogueText.text = "";
        int charIndex = 0;
        float interval = 1f / charsPerSecond;

        while (charIndex < m_fullText.Length)
        {
            if (m_fullText[charIndex] == '<')
            {
                int closeIndex = m_fullText.IndexOf('>', charIndex);
                if (closeIndex != -1)
                {
                    charIndex = closeIndex + 1;
                    m_dialogueText.text = m_fullText[..charIndex];
                    continue;
                }
            }

            charIndex++;
            m_dialogueText.text = m_fullText[..charIndex];
            yield return new WaitForSecondsRealtime(interval);
        }

        m_isTyping = false;
        if (m_continueIndicator != null)
            m_continueIndicator.SetActive(true);
    }

    public void Hide()
    {
        if (m_typingCoroutine != null)
            StopCoroutine(m_typingCoroutine);
        if (m_portraitCoroutine != null)
            StopCoroutine(m_portraitCoroutine);

        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }
}
