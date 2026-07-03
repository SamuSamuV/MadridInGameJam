using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class LevelConfig
{
    public string levelName;

    [Header("Game Ending")]
    public bool isFinalLevel = false;
    public string creditsSceneName = "Credits";

    [Header("Level Specific Dialogues")]
    [TextArea(2, 4)] public string[] dialogues;

    [Header("Rules")]
    [TextArea(2, 4)] public string rule1;
    [TextArea(2, 4)] public string rule2;

    [Header("Destinations")]
    public string destination1;
    public string destination2;
}

public class LevelIntroManager : MonoBehaviour
{
    public static LevelIntroManager Instance;

    [Header("Dialogue UI (The Lady)")]
    public RectTransform dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public float typeSpeed = 0.05f;

    [Header("Rules & Destinations UI")]
    public GameObject rulesPanel;
    public TextMeshProUGUI rule1Text;
    public TextMeshProUGUI rule2Text;
    public GameObject destinationsPanel;
    public TextMeshProUGUI destination1Text;
    public TextMeshProUGUI destination2Text;

    [Header("Map Expansion Settings")]
    public RectTransform cameraViewport;
    public RectTransform bottomBar;
    public float expandAmount = 250f;
    public float expandSpeed = 2f;

    [Header("Lady Animation Settings")]
    public float anticipationHeight = 25f;
    public float exitDropDistance = 800f;
    public float ladyAnimSpeed = 5f;

    [Header("Level Data")]
    public List<LevelConfig> levelConfigs;

    private int currentLine = 0;
    private bool isTyping = false;
    private bool isAnimatingExit = false;
    private bool isCollapsingMap = false;
    private Coroutine typingCoroutine;

    private int currentLevelIndex = 0;
    private Vector2 originalDialoguePosition;
    private Vector2 originalViewportSize;
    private Vector2 originalBarPosition;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (dialoguePanel != null)
        {
            originalDialoguePosition = dialoguePanel.anchoredPosition;
            dialoguePanel.gameObject.SetActive(false);
        }

        if (dialogueText != null) dialogueText.text = "";

        if (cameraViewport != null) originalViewportSize = cameraViewport.sizeDelta;
        if (bottomBar != null) originalBarPosition = bottomBar.anchoredPosition;

        StartCoroutine(CollapseMapAndStartIntro(0));
    }

    public void PlayNextLevelIntro()
    {
        currentLevelIndex++;
        if (currentLevelIndex >= levelConfigs.Count) return;

        StartCoroutine(CollapseMapAndStartIntro(currentLevelIndex));
    }

    private IEnumerator CollapseMapAndStartIntro(int levelIndex)
    {
        isCollapsingMap = true;

        if (dialoguePanel != null) dialoguePanel.gameObject.SetActive(false);

        if (rulesPanel != null) rulesPanel.SetActive(false);
        SetupDestinationsForLevel(levelIndex);

        if (cameraViewport != null && bottomBar != null)
        {
            Vector2 currentViewportSize = cameraViewport.sizeDelta;
            Vector2 currentBarPos = bottomBar.anchoredPosition;

            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime * expandSpeed;
                cameraViewport.sizeDelta = Vector2.Lerp(currentViewportSize, originalViewportSize, t);
                bottomBar.anchoredPosition = Vector2.Lerp(currentBarPos, originalBarPosition, t);
                yield return null;
            }
            cameraViewport.sizeDelta = originalViewportSize;
            bottomBar.anchoredPosition = originalBarPosition;
        }

        if (dialoguePanel != null) dialoguePanel.anchoredPosition = originalDialoguePosition;

        isAnimatingExit = false;
        isCollapsingMap = false;
        currentLine = 0;

        if (dialoguePanel != null) dialoguePanel.gameObject.SetActive(true);
        ShowNextLine();
    }

    private void SetupDestinationsForLevel(int levelIndex)
    {
        if (levelIndex < levelConfigs.Count)
        {
            destination1Text.text = levelConfigs[levelIndex].destination1;
            destination2Text.text = levelConfigs[levelIndex].destination2;
            if (destinationsPanel != null) destinationsPanel.SetActive(true);
        }
    }

    public void OnDialogueClicked()
    {
        if (isAnimatingExit || isCollapsingMap) return;

        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            dialogueText.text = levelConfigs[currentLevelIndex].dialogues[currentLine];
            isTyping = false;
        }
        else
        {
            currentLine++;
            ShowNextLine();
        }
    }

    private void ShowNextLine()
    {
        string[] currentDialogues = levelConfigs[currentLevelIndex].dialogues;

        if (currentLine < currentDialogues.Length)
        {
            typingCoroutine = StartCoroutine(TypeLine(currentDialogues[currentLine]));
        }
        else
        {
            if (levelConfigs[currentLevelIndex].isFinalLevel)
            {
                isAnimatingExit = true;

                if (SceneTransitionManager.Instance != null)
                {
                    SceneTransitionManager.Instance.LoadLevel(levelConfigs[currentLevelIndex].creditsSceneName);
                }
                else
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(levelConfigs[currentLevelIndex].creditsSceneName);
                }
            }
            else
            {
                isAnimatingExit = true;
                StartCoroutine(ExitAnimationAndExpand());
            }
        }
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in line.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
    }

    private IEnumerator ExitAnimationAndExpand()
    {
        Vector2 startPos = dialoguePanel.anchoredPosition;
        Vector2 upPos = startPos + new Vector2(0, anticipationHeight);
        Vector2 downPos = startPos - new Vector2(0, exitDropDistance);

        float tLady = 0;
        while (tLady < 1)
        {
            tLady += Time.deltaTime * ladyAnimSpeed;
            dialoguePanel.anchoredPosition = Vector2.Lerp(startPos, upPos, tLady);
            yield return null;
        }

        tLady = 0;
        while (tLady < 1)
        {
            tLady += Time.deltaTime * (ladyAnimSpeed * 2.5f);
            dialoguePanel.anchoredPosition = Vector2.Lerp(upPos, downPos, tLady);
            yield return null;
        }

        dialoguePanel.gameObject.SetActive(false);

        // --- ¡EL FIX MAESTRO! ---
        // Justo antes de que el telón se abra revelando el nuevo mapa,
        // le decimos al MazeLevelManager que actualice su objetivo de cámara.
        if (MazeLevelManager.Instance != null)
        {
            MazeLevelManager.Instance.TriggerPendingLevelUnlock();
        }

        if (cameraViewport != null && bottomBar != null)
        {
            Vector2 targetViewportSize = originalViewportSize + new Vector2(0, expandAmount);
            Vector2 targetBarPos = originalBarPosition - new Vector2(0, expandAmount);

            float tExpand = 0;
            while (tExpand < 1)
            {
                tExpand += Time.deltaTime * expandSpeed;
                cameraViewport.sizeDelta = Vector2.Lerp(originalViewportSize, targetViewportSize, tExpand);
                bottomBar.anchoredPosition = Vector2.Lerp(originalBarPosition, targetBarPos, tExpand);
                yield return null;
            }

            cameraViewport.sizeDelta = targetViewportSize;
            bottomBar.anchoredPosition = targetBarPos;
        }

        ShowRulesForLevel(currentLevelIndex);
    }

    private void ShowRulesForLevel(int levelIndex)
    {
        if (levelIndex < levelConfigs.Count)
        {
            rule1Text.text = levelConfigs[levelIndex].rule1;
            rule2Text.text = levelConfigs[levelIndex].rule2;
            if (rulesPanel != null) rulesPanel.SetActive(true);
        }
    }
}