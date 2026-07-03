using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MazeRailHandler : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("Maze Setup")]
    public RailNode startingNode;

    [Header("Path Painting")]
    public GameObject pathLinePrefab;
    public RectTransform linesContainer;

    [Header("Movement Settings")]
    [Tooltip("Velocidad a la que el cubo persigue al ratón. Bájalo para que pese más, súbelo para más rapidez.")]
    public float followSpeed = 25f;
    [Tooltip("Píxeles que hay que arrastrar antes de que empiece a tirar (Evita temblores).")]
    public float minDragDistanceToCommit = 20f;
    [Tooltip("Radio alrededor de la estación en el que puedes CAMBIAR de opinión de carril sin tener que soltar el ratón.")]
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

    public static MazeRailHandler Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        myRect = GetComponent<RectTransform>();
        currentNode = startingNode;

        if (currentNode != null)
        {
            myRect.localPosition = GetLocalPos(currentNode);
            dragTargetPos = myRect.localPosition;
            visitedNodes.Add(currentNode);

            if (LevelIntroManager.Instance != null)
            {
                LevelIntroManager.Instance.UpdateDestination1(currentNode.nodeName);
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

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        dragTargetPos = myRect.localPosition;
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
            (RectTransform)myRect.parent,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localMousePos))
        {
            Vector2 startPos = GetLocalPos(currentNode);
            Vector2 mouseDir = (localMousePos - startPos).normalized;
            float mouseDist = Vector2.Distance(startPos, localMousePos);
            float cubeDist = Vector2.Distance(myRect.localPosition, startPos);

            // 1. DESENGANCHE RÁPIDO: Si devuelves el ratón cerca de la estación, soltamos el raíl
            if (mouseDist < minDragDistanceToCommit)
            {
                if (targetNode != null) CancelTargetAndReturn();
            }
            else
            {
                // 2. MAGIA: ¿Podemos cambiar de ruta? 
                // Sí, si no tenemos ruta aún, O si el cubo está dentro de la zona de tolerancia (freeSwitchDistance)
                bool canSwitchTrack = (targetNode == null || cubeDist < freeSwitchDistance);

                if (canSwitchTrack)
                {
                    float bestScore = 0.3f; // Tolerancia angular amplia
                    RailNode newBestNode = targetNode;

                    foreach (RailNode connection in currentNode.connections)
                    {
                        if (connection == null || !connection.gameObject.activeInHierarchy) continue;

                        bool isPrevNode = (visitedNodes.Count > 1 && connection == visitedNodes[visitedNodes.Count - 2]);
                        bool alreadyVisited = visitedNodes.Contains(connection);

                        if (alreadyVisited && !isPrevNode) continue;

                        Vector2 trackDir = (GetLocalPos(connection) - startPos).normalized;
                        float dotProduct = Vector2.Dot(mouseDir, trackDir);

                        // Comparamos hacia dónde apunta el ratón con la dirección del raíl
                        if (dotProduct > bestScore)
                        {
                            bestScore = dotProduct;
                            newBestNode = connection;
                        }
                    }

                    // Si el jugador giró el ratón apuntando mejor hacia otro raíl válido, cambiamos al instante
                    if (newBestNode != null && newBestNode != targetNode)
                    {
                        // Devolvemos la visual de la línea anterior si estábamos retrocediendo
                        if (targetNode != null && isBacktracking && bakedLines.Count > 0)
                        {
                            bakedLines.Peek().SetActive(true);
                        }

                        // Fijamos el nuevo objetivo
                        targetNode = newBestNode;
                        isBacktracking = (visitedNodes.Count > 1 && targetNode == visitedNodes[visitedNodes.Count - 2]);

                        activeLineObj.SetActive(true);
                        if (isBacktracking && bakedLines.Count > 0)
                        {
                            bakedLines.Peek().SetActive(false);
                        }
                    }
                }

                // 3. MOVIMIENTO POR EL CARRIL ELEGIDO
                if (targetNode != null)
                {
                    GetClosestPointInfo(GetLocalPos(currentNode), GetLocalPos(targetNode), localMousePos, out Vector2 point, out float t);

                    dragTargetPos = point; // El cubo se deslizará hacia este punto

                    // Llegada a la meta
                    if (t >= 0.99f)
                    {
                        CompleteMovementToTarget();
                    }
                }
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

        // Si el jugador suelta el clic antes de llegar al objetivo, el cubo vuelve a la base
        if (targetNode != null)
        {
            CancelTargetAndReturn();
        }
    }

    private void Update()
    {
        if (currentNode != null)
        {
            if (isDragging && targetNode != null)
            {
                // MOVIMIENTO SUAVIZADO HACIA EL RATÓN
                myRect.localPosition = Vector3.Lerp(myRect.localPosition, dragTargetPos, Time.deltaTime * followSpeed);

                // Dibujar la línea unida al cubo dinámicamente
                if (isBacktracking)
                    DrawUILine(activeLineRect, GetLocalPos(targetNode), myRect.localPosition);
                else
                    DrawUILine(activeLineRect, GetLocalPos(currentNode), myRect.localPosition);
            }
            else if (!isDragging)
            {
                // RETROCESO SUAVIZADO SI SUELTA EL RATÓN
                myRect.localPosition = Vector3.Lerp(myRect.localPosition, GetLocalPos(currentNode), Time.deltaTime * followSpeed);
            }
        }
    }

    private void CompleteMovementToTarget()
    {
        if (isBacktracking)
        {
            Destroy(bakedLines.Pop());
            visitedNodes.RemoveAt(visitedNodes.Count - 1);

            if (LevelIntroManager.Instance != null)
            {
                LevelIntroManager.Instance.UpdateDestination1(targetNode.nodeName);
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
                LevelIntroManager.Instance.UpdateDestination1(targetNode.nodeName);
                LevelIntroManager.Instance.EvaluateRules(visitedNodes);
            }

            if (MazeLevelManager.Instance != null)
            {
                MazeLevelManager.Instance.CheckLevelProgression(targetNode);
            }
        }

        currentNode = targetNode;
        targetNode = null;
        activeLineObj.SetActive(false);
        dragTargetPos = GetLocalPos(currentNode);
    }

    private void CancelTargetAndReturn()
    {
        if (isBacktracking && bakedLines.Count > 0)
        {
            bakedLines.Peek().SetActive(true);
        }

        targetNode = null;
        activeLineObj.SetActive(false);
        dragTargetPos = GetLocalPos(currentNode);
    }

    private void CancelDrag()
    {
        isDragging = false;
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
        while (bakedLines.Count > 0)
        {
            Destroy(bakedLines.Pop());
        }

        visitedNodes.Clear();

        if (currentNode != null)
        {
            visitedNodes.Add(currentNode);
        }

        if (LevelIntroManager.Instance != null)
        {
            LevelIntroManager.Instance.EvaluateRules(visitedNodes);
        }

        Debug.Log("Trail cleared for the new level!");
    }
}