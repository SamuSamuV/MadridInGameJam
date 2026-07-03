using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class RailNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public GameObject tooltipObject;
    [HideInInspector] public TextMeshProUGUI tooltipText;
    [HideInInspector] public string nodeName;

    [Header("Station Setup")]
    public List<Color> stationColors = new List<Color> { Color.white };

    [Header("Connections")]
    public List<RailNode> connections;

    // --- NUEVO: Variables de efecto Hover ---
    private Vector3 originalScale;
    private Vector3 targetScale;
    private float hoverScaleMultiplier = 1.25f; // Crecerá un 25%

    private void Awake()
    {
        tooltipText = GetComponentInChildren<TextMeshProUGUI>(true);
        if (tooltipText != null)
        {
            tooltipObject = tooltipText.gameObject;
            nodeName = tooltipText.text;
            tooltipText.raycastTarget = false;
        }

        foreach (RailNode node in connections)
        {
            if (node != null && !node.connections.Contains(this))
                node.connections.Add(this);
        }

        // Guardamos su tamaño original
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    private void Start()
    {
        if (tooltipObject != null) tooltipObject.SetActive(false);
    }

    private void Update()
    {
        // Crecimiento suave
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 15f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipObject != null) tooltipObject.SetActive(true);

        targetScale = originalScale * hoverScaleMultiplier; // Se hace grande

        if (AudioManager.Instance != null) AudioManager.Instance.PlayHoverStation();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipObject != null) tooltipObject.SetActive(false);

        targetScale = originalScale; // Vuelve a la normalidad
    }

    private void OnDrawGizmos()
    {
        if (connections == null) return;
        Gizmos.color = Color.cyan;
        foreach (RailNode node in connections)
        {
            if (node != null) Gizmos.DrawLine(transform.position, node.transform.position);
        }
    }
}