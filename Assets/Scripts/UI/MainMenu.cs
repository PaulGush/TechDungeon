using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button m_startButton;
    [SerializeField] private Button m_quitButton;
    [SerializeField] private string m_gameScene = "GameScene";

    private void Awake()
    {
        m_startButton.onClick.AddListener(OnStart);
        m_quitButton.onClick.AddListener(OnQuit);
    }

    private void OnStart()
    {
        SceneManager.LoadScene(m_gameScene);
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
