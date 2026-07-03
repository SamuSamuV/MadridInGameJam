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

[System.Serializable]
public class RuleData
{
    [TextArea(2, 3)] public string ruleText;
    public RuleCondition condition = RuleCondition.None;
    public List<string> targetNodes = new List<string>();
}

[System.Serializable]
public class LevelConfig
{
    public string levelName;

    [Header("Game Ending (Credits)")]
    public string creditsSceneName = "Credits";

    [Header("Level Background")]
    public Sprite levelBackgroundSprite;

    [Header("Level Dialogues")]
    [Tooltip("Lo que dice la chica ANTES de jugar el nivel")]
    [TextArea(2, 4)] public string[] dialogues;

    [Tooltip("Lo que dice la chica DESPUÉS de jugar el nivel (Solo se usa si es el Nivel Final)")]
    [TextArea(2, 4)] public string[] outroDialogues;

    [Header("Rule 1")]
    public RuleData rule1 = new RuleData();

    [Header("Rule 2")]
    public RuleData rule2 = new RuleData();

    [Header("Destinations Setup")]
    public string destination1;
    public string destination2;
    [Tooltip("Añade aquí los colores de la parada DESTINO FINAL")]
    public List<Color> destination2Colors = new List<Color> { Color.white };
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
    public Image rule1StatusIcon;
    public Image rule2StatusIcon;
    public Sprite checkSprite;
    public Sprite crossSprite;

    public GameObject destinationsPanel;
    public TextMeshProUGUI destination1Text;
    public TextMeshProUGUI destination2Text;

    [Header("Destination Color Bars")]
    public Transform dest1ColorContainer;
    public Transform dest2ColorContainer;

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

    // NUEVO: Variable para saber si está en modo despedida
    private bool isPlayingOutro = false;

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

        isPlayingOutro = false;
        StartCoroutine(CollapseMapAndStartIntro(0));
    }

    public void PlayNextLevelIntro()
    {
        isPlayingOutro = false;
        currentLevelIndex++;
        if (currentLevelIndex >= levelConfigs.Count) return;
        StartCoroutine(CollapseMapAndStartIntro(currentLevelIndex));
    }

    // --- NUEVA FUNCIÓN: Se ejecuta cuando se completa el último nivel ---
    public void PlayOutro()
    {
        isPlayingOutro = true;
        // Reutilizamos la misma animación de colapsar el mapa para que vuelva a salir la chica
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
            UpdateColorBar(dest2ColorContainer, levelConfigs[levelIndex].destination2Colors);
            if (destinationsPanel != null) destinationsPanel.SetActive(true);
        }
    }

    public void OnDialogueClicked()
    {
        if (isAnimatingExit || isCollapsingMap) return;

        if (isTyping)
        {
            StopCoroutine(typingCoroutine);

            // Si está en despedida usa el array de Outro, si no, usa el normal
            string[] currentDialogues = isPlayingOutro ? levelConfigs[currentLevelIndex].outroDialogues : levelConfigs[currentLevelIndex].dialogues;
            dialogueText.text = currentDialogues[currentLine];

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
        // Elegimos de qué lista de textos tirar
        string[] currentDialogues = isPlayingOutro ? levelConfigs[currentLevelIndex].outroDialogues : levelConfigs[currentLevelIndex].dialogues;

        if (currentLine < currentDialogues.Length)
        {
            typingCoroutine = StartCoroutine(TypeLine(currentDialogues[currentLine]));
        }
        else
        {
            if (isPlayingOutro)
            {
                // --- SI ESTAMOS EN LA DESPEDIDA, CARGAMOS LOS CRÉDITOS AL TERMINAR DE HABLAR ---
                isAnimatingExit = true;
                if (SceneTransitionManager.Instance != null)
                    SceneTransitionManager.Instance.LoadLevel(levelConfigs[currentLevelIndex].creditsSceneName);
                else
                    UnityEngine.SceneManagement.SceneManager.LoadScene(levelConfigs[currentLevelIndex].creditsSceneName);
            }
            else
            {
                // Si es un nivel normal, hace la animación de irse y abre el mapa
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

            if (rule1Text != null && config.rule1 != null) rule1Text.text = config.rule1.ruleText;
            if (rule2Text != null && config.rule2 != null) rule2Text.text = config.rule2.ruleText;

            if (rule1StatusIcon != null) rule1StatusIcon.gameObject.SetActive(config.rule1 != null && config.rule1.condition != RuleCondition.None);
            if (rule2StatusIcon != null) rule2StatusIcon.gameObject.SetActive(config.rule2 != null && config.rule2.condition != RuleCondition.None);

            if (rulesPanel != null) rulesPanel.SetActive(true);
        }
    }

    public void UpdateDestination1(string newLocationName, List<Color> newColors)
    {
        if (destination1Text != null) destination1Text.text = newLocationName;
        UpdateColorBar(dest1ColorContainer, newColors);
    }

    private void UpdateColorBar(Transform container, List<Color> colors)
    {
        if (container == null) return;

        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        if (colors == null || colors.Count == 0) return;

        foreach (Color c in colors)
        {
            GameObject colorBlock = new GameObject("ColorBlock", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            colorBlock.transform.SetParent(container, false);
            colorBlock.GetComponent<Image>().color = c;
            colorBlock.GetComponent<LayoutElement>().flexibleWidth = 1;
        }
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
        if (rule == null || rule.condition == RuleCondition.None) return true;

        List<string> targetsClean = new List<string>();
        if (rule.targetNodes != null)
        {
            foreach (string s in rule.targetNodes) targetsClean.Add(CleanString(s));
        }

        switch (rule.condition)
        {
            case RuleCondition.MandatoryNodes:
                if (targetsClean.Count == 0) return true;
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

    public bool AreAllRulesSatisfied(List<RailNode> visitedNodes)
    {
        if (levelConfigs == null || currentLevelIndex >= levelConfigs.Count) return true;

        LevelConfig currentConfig = levelConfigs[currentLevelIndex];

        List<string> visitedNames = new List<string>();
        foreach (var node in visitedNodes)
        {
            visitedNames.Add(CleanString(node.nodeName));
        }

        bool rule1Passed = CheckRule(currentConfig.rule1, visitedNames);
        bool rule2Passed = CheckRule(currentConfig.rule2, visitedNames);

        return rule1Passed && rule2Passed;
    }

    public void FlashRules()
    {
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        Color original = Color.white;
        Color errorColor = new Color(1f, 0.5f, 0.5f);

        for (int i = 0; i < 3; i++)
        {
            if (rule1StatusIcon != null && rule1StatusIcon.sprite == crossSprite) rule1StatusIcon.color = errorColor;
            if (rule2StatusIcon != null && rule2StatusIcon.sprite == crossSprite) rule2StatusIcon.color = errorColor;

            yield return new WaitForSeconds(0.15f);

            if (rule1StatusIcon != null) rule1StatusIcon.color = original;
            if (rule2StatusIcon != null) rule2StatusIcon.color = original;

            yield return new WaitForSeconds(0.15f);
        }
    }
}