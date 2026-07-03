using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class PopupData
{
    public Sprite image;
    [TextArea(3, 5)]
    public string text;
}

[System.Serializable]
public class MazeLevel
{
    public string levelName;

    [Header("Progression")]
    public RailNode levelEndNode;
    public GameObject nextLevelContainer;

    [Header("Game Ending")]
    public bool isFinalLevel = false;

    [Header("Camera Automation")]
    public float targetZoomScale = 1f;
    [Tooltip("El objeto vacío dentro del mapa que marcará el centro de la cámara")]
    public RectTransform cameraFocusPoint;

    [Header("Random Popups")]
    public List<PopupData> popupOptions;
}

public class MazeLevelManager : MonoBehaviour
{
    public static MazeLevelManager Instance;

    [Header("Maze Components")]
    public RectTransform mazeContainer;
    public RectTransform cameraViewport;
    public float transitionSpeed = 2f;

    [Header("Initial Camera Setup")]
    public RectTransform startingFocusPoint;

    [Header("UI Popup Panel")]
    public GameObject popupPanel;
    public Image popupImage;
    public TextMeshProUGUI popupText;

    [Header("Progression Levels")]
    public List<MazeLevel> levels;

    private float currentTargetScale = 1f;
    private RectTransform currentFocusPoint;
    private MazeLevel pendingLevelToUnlock;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        currentTargetScale = mazeContainer.localScale.x;
        currentFocusPoint = startingFocusPoint;

        if (popupPanel != null) popupPanel.SetActive(false);
    }

    public void CheckLevelProgression(RailNode reachedNode)
    {
        foreach (MazeLevel level in levels)
        {
            if (level.levelEndNode == reachedNode)
            {
                if (level.isFinalLevel || (level.nextLevelContainer != null && !level.nextLevelContainer.activeSelf))
                {
                    ShowCompletionPopup(level);
                }
            }
        }
    }

    private void ShowCompletionPopup(MazeLevel level)
    {
        pendingLevelToUnlock = level;

        if (level.popupOptions != null && level.popupOptions.Count > 0)
        {
            int randomIndex = Random.Range(0, level.popupOptions.Count);
            PopupData selectedPopup = level.popupOptions[randomIndex];

            if (popupImage != null) popupImage.sprite = selectedPopup.image;
            if (popupText != null) popupText.text = selectedPopup.text;
        }

        popupPanel.SetActive(true);
    }

    public void OnClosePopupClicked()
    {
        popupPanel.SetActive(false);

        if (pendingLevelToUnlock != null)
        {
            if (pendingLevelToUnlock.isFinalLevel)
            {
                return;
            }

            if (MazeRailHandler.Instance != null)
            {
                MazeRailHandler.Instance.ClearTrail();
            }

            if (LevelIntroManager.Instance != null)
            {
                LevelIntroManager.Instance.PlayNextLevelIntro();
            }
        }
    }

    public void TriggerPendingLevelUnlock()
    {
        if (pendingLevelToUnlock != null)
        {
            UnlockLevel(pendingLevelToUnlock);
            pendingLevelToUnlock = null;
        }
    }

    private void UnlockLevel(MazeLevel level)
    {
        if (level.nextLevelContainer != null) level.nextLevelContainer.SetActive(true);

        currentTargetScale = level.targetZoomScale;
        if (level.cameraFocusPoint != null) currentFocusPoint = level.cameraFocusPoint;
    }

    private void Update()
    {
        Vector3 targetScaleVec = new Vector3(currentTargetScale, currentTargetScale, 1f);
        mazeContainer.localScale = Vector3.Lerp(mazeContainer.localScale, targetScaleVec, Time.deltaTime * transitionSpeed);

        if (currentFocusPoint != null && cameraViewport != null)
        {
            Vector2 viewportCenter = new Vector2(
                (0.5f - cameraViewport.pivot.x) * cameraViewport.rect.width,
                (0.5f - cameraViewport.pivot.y) * cameraViewport.rect.height
            );

            Vector2 focusLocalPos = mazeContainer.InverseTransformPoint(currentFocusPoint.position);
            Vector2 targetPos = viewportCenter - (focusLocalPos * currentTargetScale);

            mazeContainer.localPosition = Vector3.Lerp(mazeContainer.localPosition, (Vector3)targetPos, Time.deltaTime * transitionSpeed);
        }
    }
}