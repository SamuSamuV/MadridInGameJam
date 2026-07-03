using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class RailNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public GameObject tooltipObject;
    [HideInInspector] public TextMeshProUGUI tooltipText;
    [HideInInspector] public string nodeName;

    [Header("Connections")]
    [Tooltip("Drag the nodes you can travel to from this point")]
    public List<RailNode> connections;

    private void Awake()
    {
        tooltipText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (tooltipText != null)
        {
            tooltipObject = tooltipText.gameObject;
            nodeName = tooltipText.text;
            tooltipText.raycastTarget = false;
        }
        else
        {
            Debug.LogError($"¡Atención en {gameObject.name}! Esta parada no tiene ningún hijo con TextMeshPro.", this);
        }

        foreach (RailNode node in connections)
        {
            if (node != null && !node.connections.Contains(this))
            {
                node.connections.Add(this);
            }
        }
    }

    private void Start()
    {
        if (tooltipObject != null)
        {
            tooltipObject.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipObject != null) tooltipObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipObject != null) tooltipObject.SetActive(false);
    }

    private void OnDrawGizmos()
    {
        if (connections == null) return;
        Gizmos.color = Color.cyan;
        foreach (RailNode node in connections)
        {
            if (node != null)
            {
                Gizmos.DrawLine(transform.position, node.transform.position);
            }
        }
    }
}