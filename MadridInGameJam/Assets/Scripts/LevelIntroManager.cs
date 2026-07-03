using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public enum RuleCondition
{
    None,
    MandatoryNodes,
    ForbiddenConnections,
    ForbiddenNodes
}

// Estructura de cada Regla
[System.Serializable]
public class RuleData
{
    [TextArea(2, 3)] public string ruleText;
    public RuleCondition condition;
    [Tooltip("Escribe los nombres exactos de las paradas. Ej: Goya, Cuatro Caminos...")]
    public List<string> targetNodes;
}

[System.Serializable]
public class LevelConfig
{
    public string levelName;

    [Header("Game Ending")]
    public bool isFinalLevel = false;
    public string creditsSceneName = "Credits";

    [Header("Level Background")]
    public Sprite levelBackgroundSprite;

    [Header("Level Specific Dialogues")]
    [TextArea(2, 4)] public string[] dialogues;

    [Header("Rule 1")]
    public RuleData rule1;

    [Header("Rule 2")]
    public RuleData rule2;

    [Header("Destinations")]
    public string destination1;
    public string destination2;
}

public class LevelIntroManager : MonoBehaviour
{
    public static LevelIntroManager Instance;

    [Header("Background UI")]
    public Image backgroundUI;

    [Header("Dialogue UI (The Lady)")]
    public RectTransform dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public float typeSpeed = 0.05f;

    [Header("Rules & Destinations UI")]
    public GameObject rulesPanel;
    public TextMeshProUGUI rule1Text;
    public TextMeshProUGUI rule2Text;

    [Header("Rule Status Icons")]
    public Image rule1StatusIcon;
    public Image rule2StatusIcon;
    public Sprite checkSprite;
    public Sprite crossSprite;

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

        if (backgroundUI != null && levelConfigs[levelIndex].levelBackgroundSprite != null)
        {
            backgroundUI.sprite = levelConfigs[levelIndex].levelBackgroundSprite;
        }

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
            if (destination2Text != null) destination2Text.text = levelConfigs[levelIndex].destination2;
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
                    SceneTransitionManager.Instance.LoadLevel(levelConfigs[currentLevelIndex].creditsSceneName);
                else
                    UnityEngine.SceneManagement.SceneManager.LoadScene(levelConfigs[currentLevelIndex].creditsSceneName);
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

        if (MazeLevelManager.Instance != null)
            MazeLevelManager.Instance.TriggerPendingLevelUnlock();

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

        if (MazeRailHandler.Instance != null)
            EvaluateRules(MazeRailHandler.Instance.GetVisitedNodes());
    }

    private void ShowRulesForLevel(int levelIndex)
    {
        if (levelIndex < levelConfigs.Count)
        {
            LevelConfig config = levelConfigs[levelIndex];

            if (rule1Text != null) rule1Text.text = config.rule1.ruleText;
            if (rule2Text != null) rule2Text.text = config.rule2.ruleText;

            if (rule1StatusIcon != null) rule1StatusIcon.gameObject.SetActive(config.rule1.condition != RuleCondition.None);
            if (rule2StatusIcon != null) rule2StatusIcon.gameObject.SetActive(config.rule2.condition != RuleCondition.None);

            if (rulesPanel != null) rulesPanel.SetActive(true);
        }
    }

    public void UpdateDestination1(string newLocationName)
    {
        if (destination1Text != null) destination1Text.text = newLocationName;
    }

    private string CleanString(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return input.Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\u200B", "").ToLowerInvariant();
    }

    public void EvaluateRules(List<RailNode> visitedNodes)
    {
        LevelConfig currentConfig = levelConfigs[currentLevelIndex];

        List<string> visitedNames = new List<string>();
        foreach (var node in visitedNodes)
        {
            visitedNames.Add(CleanString(node.nodeName));
        }

        if (rule1StatusIcon != null)
            rule1StatusIcon.sprite = CheckRule(currentConfig.rule1, visitedNames) ? checkSprite : crossSprite;

        if (rule2StatusIcon != null)
            rule2StatusIcon.sprite = CheckRule(currentConfig.rule2, visitedNames) ? checkSprite : crossSprite;
    }

    private bool CheckRule(RuleData rule, List<string> visitedNames)
    {
        if (rule.condition == RuleCondition.None) return true;

        List<string> targetsClean = new List<string>();
        if (rule.targetNodes != null)
        {
            foreach (string s in rule.targetNodes) targetsClean.Add(CleanString(s));
        }

        switch (rule.condition)
        {
            case RuleCondition.MandatoryNodes:
                if (targetsClean.Count == 0) return false;
                foreach (string mandatory in targetsClean)
                {
                    if (!visitedNames.Contains(mandatory)) return false;
                }
                return true;

            case RuleCondition.ForbiddenConnections:
                if (targetsClean.Count == 0) return true;
                for (int i = 0; i < visitedNames.Count - 1; i++)
                {
                    string nodeA = visitedNames[i];
                    string nodeB = visitedNames[i + 1];

                    if (targetsClean.Contains(nodeA) && targetsClean.Contains(nodeB))
                    {
                        return false;
                    }
                }
                return true;

            case RuleCondition.ForbiddenNodes:
                if (targetsClean.Count == 0) return true;
                foreach (string visited in visitedNames)
                {
                    if (targetsClean.Contains(visited)) return false;
                }
                return true;
        }

        return true;
    }
}