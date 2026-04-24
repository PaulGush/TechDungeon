using System.Collections;
using Gameplay.ObjectPool;
using PlayerObject;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityServiceLocator;

public class DeathScreen : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerDeathEffect m_deathEffect;
    [SerializeField] private PlayerAnimationController m_playerAnimator;
    [SerializeField] private WeaponHolder m_weaponHolder;

    [Header("Buttons")]
    [SerializeField] private Button m_newRunButton;
    [SerializeField] private Button m_mainMenuButton;

    [Header("Animation")]
    [SerializeField] private float m_buttonDelay = 0.3f;
    [SerializeField] private float m_buttonAnimDuration = 0.4f;
    [SerializeField] private float m_slideDistance = 60f;

    [Header("Scenes")]
    [SerializeField] private string m_mainMenuScene = "MainMenu";

    private CanvasGroup[] m_buttonGroups;
    private RectTransform[] m_buttonRects;
    private float m_originalFixedDeltaTime;

    private void Awake()
    {
        m_originalFixedDeltaTime = Time.fixedDeltaTime;

        m_buttonGroups = new[]
        {
            GetOrAddCanvasGroup(m_newRunButton),
            GetOrAddCanvasGroup(m_mainMenuButton)
        };

        m_buttonRects = new[]
        {
            m_newRunButton.GetComponent<RectTransform>(),
            m_mainMenuButton.GetComponent<RectTransform>()
        };

        m_newRunButton.onClick.AddListener(OnNewRun);
        m_mainMenuButton.onClick.AddListener(OnMainMenu);

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (m_newRunButton != null) m_newRunButton.onClick.RemoveListener(OnNewRun);
        if (m_mainMenuButton != null) m_mainMenuButton.onClick.RemoveListener(OnMainMenu);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        StartCoroutine(AnimateButtonsIn());
    }

    private IEnumerator AnimateButtonsIn()
    {
        for (int i = 0; i < m_buttonGroups.Length; i++)
        {
            m_buttonGroups[i].alpha = 0f;
            m_buttonGroups[i].interactable = false;
        }

        for (int i = 0; i < m_buttonGroups.Length; i++)
        {
            StartCoroutine(AnimateButton(m_buttonGroups[i], m_buttonRects[i]));
            yield return new WaitForSecondsRealtime(m_buttonDelay);
        }
    }

    private IEnumerator AnimateButton(CanvasGroup group, RectTransform rect)
    {
        Vector2 startPos = rect.anchoredPosition + Vector2.down * m_slideDistance;
        Vector2 endPos = rect.anchoredPosition;
        rect.anchoredPosition = startPos;

        float elapsed = 0f;
        while (elapsed < m_buttonAnimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / m_buttonAnimDuration);

            float easedT = 1f - (1f - t) * (1f - t);

            group.alpha = easedT;
            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, easedT);

            yield return null;
        }

        group.alpha = 1f;
        group.interactable = true;
        rect.anchoredPosition = endPos;
    }

    private void OnNewRun()
    {
        ResetRun();
    }

    private void OnMainMenu()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = m_originalFixedDeltaTime;
        ServiceLocator.Global.Get<ObjectPool>().ClearAll();
        SceneManager.LoadScene(m_mainMenuScene);
    }

    private void ResetRun()
    {
        m_deathEffect.Reset();
        m_playerAnimator.ResetAnimator();

        ServiceLocator.Global.Get<ObjectPool>().ClearAll();
        ServiceLocator.Global.Get<ItemManager>().Reset();
        ServiceLocator.Global.Get<AmmoManager>().Reset();
        ServiceLocator.Global.Get<CreditManager>().Reset();
        m_weaponHolder.Reset();
        ServiceLocator.Global.Get<RoomManager>().ResetToStartingRoom();

        gameObject.SetActive(false);
    }

    private static CanvasGroup GetOrAddCanvasGroup(Component component)
    {
        if (!component.TryGetComponent(out CanvasGroup group))
            group = component.gameObject.AddComponent<CanvasGroup>();
        return group;
    }
}
