using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    [Header("Transition Settings")]
    public CanvasGroup fadeGroup;
    public float fadeDuration = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
    }

    public void LoadLevel(string sceneName)
    {
        StartCoroutine(TransitionRoutine(sceneName));
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        fadeGroup.blocksRaycasts = true;
        float time = 0;

        while (time < fadeDuration)
        {
            fadeGroup.alpha = Mathf.Lerp(0f, 1f, time / fadeDuration);
            time += Time.deltaTime;
            yield return null;
        }
        fadeGroup.alpha = 1f;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        time = 0;
        while (time < fadeDuration)
        {
            fadeGroup.alpha = Mathf.Lerp(1f, 0f, time / fadeDuration);
            time += Time.deltaTime;
            yield return null;
        }
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
    }
}