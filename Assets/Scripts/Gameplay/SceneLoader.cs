using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void LoadSceneAsync(string sceneName, Action onComplete = null)
    {
        StartCoroutine(LoadSceneAsyncCoroutine(sceneName, onComplete));
    }

    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator LoadSceneAsyncCoroutine(string sceneName, Action onComplete)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        if (asyncLoad == null)
        {
            Debug.LogError($"Failed to load scene: {sceneName}");
            yield break;
        }

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        onComplete?.Invoke();
    }
}
