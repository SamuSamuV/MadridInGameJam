using UnityEngine;
using UnityEngine.EventSystems;

public class RailHandler : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("Configuración del Raíl")]
    [Tooltip("Arrastra aquí los RectTransforms de los checkpoints en orden")]
    public RectTransform[] checkpoints;

    [Header("Ajustes al soltar (Snapping)")]
    public bool snapToCheckpoint = true;
    public float snapSpeed = 15f;

    private RectTransform myRect;
    private int currentTargetIndex = -1;

    void Awake()
    {
        myRect = GetComponent<RectTransform>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        currentTargetIndex = -1;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)myRect.parent,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPosition))
        {
            myRect.localPosition = GetClosestPointOnRails(localPointerPosition);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (snapToCheckpoint)
        {
            currentTargetIndex = GetClosestCheckpointIndex(myRect.localPosition);

            Debug.Log("Llegaste al checkpoint: " + checkpoints[currentTargetIndex].gameObject.name);
        }
    }

    void Update()
    {
        if (currentTargetIndex != -1)
        {
            myRect.localPosition = Vector3.Lerp(
                myRect.localPosition,
                checkpoints[currentTargetIndex].localPosition,
                Time.deltaTime * snapSpeed
            );
        }
    }

    private Vector2 GetClosestPointOnRails(Vector2 pointerPos)
    {
        if (checkpoints.Length < 2) return pointerPos;

        Vector2 closestPoint = Vector2.zero;
        float shortestDistance = float.MaxValue;

        for (int i = 0; i < checkpoints.Length - 1; i++)
        {
            Vector2 a = checkpoints[i].localPosition;
            Vector2 b = checkpoints[i + 1].localPosition;

            Vector2 pointOnSegment = GetClosestPointOnSegment(a, b, pointerPos);
            float distance = Vector2.Distance(pointerPos, pointOnSegment);

            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                closestPoint = pointOnSegment;
            }
        }
        return closestPoint;
    }

    private Vector2 GetClosestPointOnSegment(Vector2 a, Vector2 b, Vector2 p)
    {
        Vector2 ap = p - a;
        Vector2 ab = b - a;
        float magnitudeAB = ab.sqrMagnitude;
        float abapProduct = Vector2.Dot(ap, ab);
        float distance = abapProduct / magnitudeAB;

        if (distance < 0) return a;
        if (distance > 1) return b;

        return a + ab * distance;
    }

    private int GetClosestCheckpointIndex(Vector2 currentPos)
    {
        int bestIndex = 0;
        float minDistance = float.MaxValue;
        for (int i = 0; i < checkpoints.Length; i++)
        {
            float dist = Vector2.Distance(currentPos, (Vector2)checkpoints[i].localPosition);
            if (dist < minDistance)
            {
                minDistance = dist;
                bestIndex = i;
            }
        }
        return bestIndex;
    }
}