using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MazeRailHandler : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Maze Setup")]
    public RailNode startingNode;

    [Header("Path Painting")]
    public GameObject pathLinePrefab;
    public RectTransform linesContainer;

    [Header("Movement Settings")]
    public float followSpeed = 25f;
    public float minDragDistanceToCommit = 20f;
    public float freeSwitchDistance = 80f;
    public float maxDragDistance = 250f;

    private RectTransform myRect;
    private RailNode currentNode;
    private RailNode targetNode;

    private List<RailNode> visitedNodes = new List<RailNode>();
    private Stack<GameObject> bakedLines = new Stack<GameObject>();
    private GameObject activeLineObj;
    private RectTransform activeLineRect;
    private bool isBacktracking = false;

    private bool isDragging = false;
    private Vector2 dragTargetPos;
    private Vector2 dragOffset;

    // --- NUEVO: Hover del jugador ---
    private Vector3 originalScale;
    private Vector3 targetScale;
    private float hoverScaleMultiplier = 1.15f;

    public static MazeRailHandler Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        myRect = GetComponent<RectTransform>();

        originalScale = myRect.localScale;
        targetScale = originalScale;

        currentNode = startingNode;

        if (currentNode != null)
        {
            myRect.localPosition = GetLocalPos(currentNode);
            dragTargetPos = myRect.localPosition;
            visitedNodes.Add(currentNode);

            if (LevelIntroManager.Instance != null)
            {
                LevelIntroManager.Instance.UpdateDestination1(currentNode.nodeName, currentNode.stationColors);
                LevelIntroManager.Instance.EvaluateRules(visitedNodes);
            }
        }

        activeLineObj = Instantiate(pathLinePrefab, linesContainer);
        activeLineRect = activeLineObj.GetComponent<RectTransform>();
        activeLineObj.SetActive(false);
    }

    public List<RailNode> GetVisitedNodes()
    {
        return visitedNodes;
    }

    // Efecto Hover visual sobre el propio jugador
    public void OnPointerEnter(PointerEventData eventData) { targetScale = originalScale * hoverScaleMultiplier; }
    public void OnPointerExit(PointerEventData eventData) { if (!isDragging) targetScale = originalScale; }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        dragTargetPos = myRect.localPosition;
        targetScale = originalScale * hoverScaleMultiplier;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayPlayerClick();

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)myRect.parent, eventData.position, eventData.pressEventCamera, out Vector2 localMousePos))
        {
            dragOffset = (Vector2)myRect.localPosition - localMousePos;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        Vector2 playerScreenPos = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, myRect.position);
        if (Vector2.Distance(eventData.position, playerScreenPos) > maxDragDistance)
        {
            CancelDrag();
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)myRect.parent, eventData.position, eventData.pressEventCamera, out Vector2 rawLocalMousePos))
        {
            Vector2 localMousePos = rawLocalMousePos + dragOffset;
            Vector2 startPos = GetLocalPos(currentNode);
            Vector2 mouseDir = (localMousePos - startPos).normalized;
            float mouseDist = Vector2.Distance(startPos, localMousePos);
            float cubeDist = Vector2.Distance(myRect.localPosition, startPos);

            if (mouseDist < minDragDistanceToCommit)
            {
                if (targetNode != null) CancelTargetAndReturn();
            }
            else
            {
                bool canSwitchTrack = (targetNode == null || cubeDist < freeSwitchDistance);

                if (canSwitchTrack)
                {
                    float bestScore = 0.3f;
                    RailNode newBestNode = targetNode;

                    foreach (RailNode connection in currentNode.connections)
                    {
                        if (connection == null || !connection.gameObject.activeInHierarchy) continue;

                        bool isPrevNode = (visitedNodes.Count > 1 && connection == visitedNodes[visitedNodes.Count - 2]);
                        bool alreadyVisited = visitedNodes.Contains(connection);

                        if (alreadyVisited && !isPrevNode) continue;

                        Vector2 trackDir = (GetLocalPos(connection) - startPos).normalized;
                        float dotProduct = Vector2.Dot(mouseDir, trackDir);

                        if (dotProduct > bestScore)
                        {
                            bestScore = dotProduct;
                            newBestNode = connection;
                        }
                    }

                    if (newBestNode != null && newBestNode != targetNode)
                    {
                        if (targetNode != null && isBacktracking && bakedLines.Count > 0)
                            bakedLines.Peek().SetActive(true);

                        targetNode = newBestNode;
                        isBacktracking = (visitedNodes.Count > 1 && targetNode == visitedNodes[visitedNodes.Count - 2]);

                        activeLineObj.SetActive(true);
                        if (isBacktracking && bakedLines.Count > 0)
                            bakedLines.Peek().SetActive(false);
                    }
                }

                if (targetNode != null)
                {
                    GetClosestPointInfo(GetLocalPos(currentNode), GetLocalPos(targetNode), localMousePos, out Vector2 point, out float t);
                    dragTargetPos = point;

                    if (t >= 0.99f) CompleteMovementToTarget();
                }
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        targetScale = originalScale;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayPlayerRelease();

        if (targetNode != null) CancelTargetAndReturn();
    }

    private void Update()
    {
        myRect.localScale = Vector3.Lerp(myRect.localScale, targetScale, Time.deltaTime * 15f);

        if (currentNode != null)
        {
            if (isDragging && targetNode != null)
            {
                myRect.localPosition = Vector3.Lerp(myRect.localPosition, dragTargetPos, Time.deltaTime * followSpeed);

                if (isBacktracking) DrawUILine(activeLineRect, GetLocalPos(targetNode), myRect.localPosition);
                else DrawUILine(activeLineRect, GetLocalPos(currentNode), myRect.localPosition);
            }
            else if (!isDragging)
            {
                myRect.localPosition = Vector3.Lerp(myRect.localPosition, GetLocalPos(currentNode), Time.deltaTime * followSpeed);
            }
        }
    }

    private void CompleteMovementToTarget()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPassStation();

        if (isBacktracking)
        {
            Destroy(bakedLines.Pop());
            visitedNodes.RemoveAt(visitedNodes.Count - 1);

            if (LevelIntroManager.Instance != null)
            {
                LevelIntroManager.Instance.UpdateDestination1(targetNode.nodeName, targetNode.stationColors);
                LevelIntroManager.Instance.EvaluateRules(visitedNodes);
            }
        }
        else
        {
            GameObject newBakedLine = Instantiate(pathLinePrefab, linesContainer);
            DrawUILine(newBakedLine.GetComponent<RectTransform>(), GetLocalPos(currentNode), GetLocalPos(targetNode));
            bakedLines.Push(newBakedLine);
            visitedNodes.Add(targetNode);

            if (LevelIntroManager.Instance != null)
            {
                LevelIntroManager.Instance.UpdateDestination1(targetNode.nodeName, targetNode.stationColors);
                LevelIntroManager.Instance.EvaluateRules(visitedNodes);
            }

            if (MazeLevelManager.Instance != null)
            {
                MazeLevelManager.Instance.CheckLevelProgression(targetNode);
            }
        }

        if (MainMenuManager.Instance != null) MainMenuManager.Instance.OnNodeReached(targetNode);

        currentNode = targetNode;
        targetNode = null;
        activeLineObj.SetActive(false);
        dragTargetPos = GetLocalPos(currentNode);
    }

    private void CancelTargetAndReturn()
    {
        if (isBacktracking && bakedLines.Count > 0) bakedLines.Peek().SetActive(true);
        targetNode = null;
        activeLineObj.SetActive(false);
        dragTargetPos = GetLocalPos(currentNode);
    }

    private void CancelDrag()
    {
        isDragging = false;
        targetScale = originalScale;
        CancelTargetAndReturn();
    }

    private void DrawUILine(RectTransform lineRect, Vector2 pointA, Vector2 pointB)
    {
        Vector2 direction = pointB - pointA;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        lineRect.sizeDelta = new Vector2(distance, lineRect.sizeDelta.y);
        lineRect.localEulerAngles = new Vector3(0, 0, angle);
        lineRect.localPosition = pointA + direction / 2f;
    }

    private Vector2 GetLocalPos(RailNode node)
    {
        return (Vector2)myRect.parent.InverseTransformPoint(node.transform.position);
    }

    private void GetClosestPointInfo(Vector2 start, Vector2 end, Vector2 mousePoint, out Vector2 projectedPoint, out float percentage)
    {
        Vector2 ap = mousePoint - start;
        Vector2 ab = end - start;
        float magnitudeAB = ab.sqrMagnitude;

        if (magnitudeAB == 0)
        {
            projectedPoint = start;
            percentage = 0;
            return;
        }

        percentage = Vector2.Dot(ap, ab) / magnitudeAB;
        percentage = Mathf.Clamp01(percentage);
        projectedPoint = start + ab * percentage;
    }

    public void ClearTrail()
    {
        while (bakedLines.Count > 0) Destroy(bakedLines.Pop());
        visitedNodes.Clear();
        if (currentNode != null) visitedNodes.Add(currentNode);
        if (LevelIntroManager.Instance != null) LevelIntroManager.Instance.EvaluateRules(visitedNodes);
    }

    public void ResetToStart()
    {
        ClearTrail();
        isDragging = false;
        targetScale = originalScale;
        targetNode = null;
        currentNode = startingNode;

        if (currentNode != null)
        {
            myRect.localPosition = GetLocalPos(currentNode);
            dragTargetPos = myRect.localPosition;
        }

        if (activeLineObj != null) activeLineObj.SetActive(false);
    }
}