using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class MazeLevel
{
    public string levelName;

    [Header("Progression Triggers")]
    public RailNode levelEndNode;
    public GameObject nextLevelContainer;

    [Header("Camera Animation Targets")]
    [Tooltip("Target scale for the zoom out (e.g., 0.7)")]
    public float targetZoomScale = 1f;

    [Tooltip("Target X and Y position to center the new area")]
    public Vector2 targetPosition;

    [Header("Completion Popup Customization")]
    public Sprite completionImage;
    [TextArea(3, 5)]
    public string completionText;
}

public class MazeLevelManager : MonoBehaviour
{
    public static MazeLevelManager Instance;

    [Header("Maze Components")]
    public RectTransform mazeContainer;
    public float transitionSpeed = 2f;

    [Header("UI Popup Panel")]
    public GameObject popupPanel;
    public Image popupImage;
    public TMPro.TextMeshProUGUI popupText;

    [Header("Progression Levels")]
    public List<MazeLevel> levels;

    private float currentTargetScale = 1f;
    private Vector2 currentTargetPosition;
    private MazeLevel pendingLevelToUnlock;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        currentTargetScale = mazeContainer.localScale.x;
        currentTargetPosition = mazeContainer.localPosition;

        if (popupPanel != null) popupPanel.SetActive(false);
    }

    public void CheckLevelProgression(RailNode reachedNode)
    {
        foreach (MazeLevel level in levels)
        {
            if (level.levelEndNode == reachedNode && level.nextLevelContainer != null)
            {
                if (!level.nextLevelContainer.activeSelf)
                {
                    ShowCompletionPopup(level);
                }
            }
        }
    }

    private void ShowCompletionPopup(MazeLevel level)
    {
        pendingLevelToUnlock = level;

        if (popupImage != null) popupImage.sprite = level.completionImage;
        if (popupText != null) popupText.text = level.completionText;

        popupPanel.SetActive(true);
    }

    public void OnClosePopupClicked()
    {
        popupPanel.SetActive(false);

        if (pendingLevelToUnlock != null)
        {
            UnlockLevel(pendingLevelToUnlock);
            pendingLevelToUnlock = null;
        }
    }

    private void UnlockLevel(MazeLevel level)
    {
        level.nextLevelContainer.SetActive(true);

        currentTargetScale = level.targetZoomScale;
        currentTargetPosition = level.targetPosition;

        Debug.Log($"Unlocked Level: {level.levelName}! Animating camera to Scale {currentTargetScale} and Pos {currentTargetPosition}");
    }

    private void Update()
    {
        Vector3 targetScaleVec = new Vector3(currentTargetScale, currentTargetScale, 1f);
        mazeContainer.localScale = Vector3.Lerp(mazeContainer.localScale, targetScaleVec, Time.deltaTime * transitionSpeed);

        mazeContainer.localPosition = Vector3.Lerp(mazeContainer.localPosition, (Vector3)currentTargetPosition, Time.deltaTime * transitionSpeed);
    }
}