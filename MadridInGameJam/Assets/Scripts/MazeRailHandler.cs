using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MazeRailHandler : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("Maze Setup")]
    public RailNode startingNode;

    [Header("Path Painting")]
    [Tooltip("The prefab for the UI Line (Pivot must be 0.5, 0.5)")]
    public GameObject pathLinePrefab;
    [Tooltip("An empty RectTransform inside MazeContainer to hold the drawn lines behind the nodes")]
    public RectTransform linesContainer;

    [Header("Settings")]
    public float snapSpeed = 15f;
    public float maxDragDistance = 150f;

    private RectTransform myRect;
    private RailNode currentNode;
    private RailNode targetNode;

    private List<RailNode> visitedNodes = new List<RailNode>();
    private Stack<GameObject> bakedLines = new Stack<GameObject>();
    private GameObject activeLineObj;
    private RectTransform activeLineRect;
    private bool isBacktracking = false;

    private bool isDragging = false;
    private bool dragCanceled = false;

    private void Start()
    {
        myRect = GetComponent<RectTransform>();
        currentNode = startingNode;

        if (currentNode != null)
        {
            myRect.localPosition = GetLocalPos(currentNode);
            visitedNodes.Add(currentNode);
        }

        activeLineObj = Instantiate(pathLinePrefab, linesContainer);
        activeLineRect = activeLineObj.GetComponent<RectTransform>();
        activeLineObj.SetActive(false);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        dragCanceled = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || dragCanceled) return;

        Vector2 playerScreenPos = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, myRect.position);

        if (Vector2.Distance(eventData.position, playerScreenPos) > maxDragDistance)
        {
            CancelDrag("Cursor moved too far physically on screen!");
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)myRect.parent,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localMousePos))
        {
            if (targetNode == null)
            {
                float bestDistance = float.MaxValue;
                RailNode bestNode = null;
                Vector2 bestPoint = GetLocalPos(currentNode);

                foreach (RailNode connection in currentNode.connections)
                {
                    if (connection == null || !connection.gameObject.activeInHierarchy) continue;

                    bool isPreviousNode = (visitedNodes.Count > 1 && connection == visitedNodes[visitedNodes.Count - 2]);
                    bool alreadyVisited = visitedNodes.Contains(connection);

                    if (alreadyVisited && !isPreviousNode) continue;

                    GetClosestPointInfo(GetLocalPos(currentNode), GetLocalPos(connection), localMousePos, out Vector2 point, out float t);
                    float distanceToMouse = Vector2.Distance(localMousePos, point);

                    if (distanceToMouse < bestDistance)
                    {
                        bestDistance = distanceToMouse;
                        bestNode = connection;
                        bestPoint = point;
                    }
                }

                if (bestNode != null && Vector2.Distance(bestPoint, GetLocalPos(currentNode)) > 1f)
                {
                    targetNode = bestNode;
                    myRect.localPosition = bestPoint;

                    isBacktracking = (visitedNodes.Count > 1 && targetNode == visitedNodes[visitedNodes.Count - 2]);

                    activeLineObj.SetActive(true);

                    if (isBacktracking)
                    {
                        bakedLines.Peek().SetActive(false);
                    }
                }
            }

            else
            {
                GetClosestPointInfo(GetLocalPos(currentNode), GetLocalPos(targetNode), localMousePos, out Vector2 point, out float t);
                myRect.localPosition = point;

                if (isBacktracking)
                {
                    DrawUILine(activeLineRect, GetLocalPos(targetNode), point);
                }

                else
                {
                    DrawUILine(activeLineRect, GetLocalPos(currentNode), point);
                }

                if (t >= 0.99f)
                {
                    if (isBacktracking)
                    {
                        Destroy(bakedLines.Pop());
                        visitedNodes.RemoveAt(visitedNodes.Count - 1);
                    }

                    else
                    {
                        GameObject newBakedLine = Instantiate(pathLinePrefab, linesContainer);
                        DrawUILine(newBakedLine.GetComponent<RectTransform>(), GetLocalPos(currentNode), GetLocalPos(targetNode));
                        bakedLines.Push(newBakedLine);
                        visitedNodes.Add(targetNode);

                        if (MazeLevelManager.Instance != null)
                        {
                            MazeLevelManager.Instance.CheckLevelProgression(targetNode);
                        }
                    }

                    currentNode = targetNode;
                    targetNode = null;
                    activeLineObj.SetActive(false);
                }

                else if (t <= 0.01f)
                {
                    if (isBacktracking && bakedLines.Count > 0)
                    {
                        bakedLines.Peek().SetActive(true);
                    }
                    targetNode = null;
                    activeLineObj.SetActive(false);
                }
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragCanceled) return;

        isDragging = false;
        if (targetNode != null)
        {
            CancelDrag("Released early!");
        }
    }

    private void Update()
    {
        if (!isDragging && currentNode != null)
        {
            myRect.localPosition = Vector3.Lerp(
                myRect.localPosition,
                GetLocalPos(currentNode),
                Time.deltaTime * snapSpeed
            );
        }
    }

    private void CancelDrag(string reason)
    {
        dragCanceled = true;
        isDragging = false;

        if (targetNode != null && isBacktracking && bakedLines.Count > 0)
        {
            bakedLines.Peek().SetActive(true);
        }

        targetNode = null;
        activeLineObj.SetActive(false);
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
}