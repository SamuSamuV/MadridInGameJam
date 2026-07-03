using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class RailNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Checkpoint UI")]
    [Tooltip("Arrastra aquí el objeto HIJO que tiene el texto (NO la parada entera)")]
    public GameObject tooltipObject;
    [Tooltip("Arrastra aquí el texto para que el script lo lea automáticamente")]
    public TextMeshProUGUI tooltipText;

    // Lo ocultamos en el inspector porque el script lo leerá automáticamente del texto
    [HideInInspector]
    public string nodeName;

    [Header("Connections")]
    [Tooltip("Drag the nodes you can travel to from this point")]
    public List<RailNode> connections;

    private void Awake()
    {
        // 1. Magia: Leemos el nombre que ya has escrito en Unity ANTES de empezar
        if (tooltipText != null)
        {
            nodeName = tooltipText.text;

            // ¡EL FIX DEL PARPADEO! Evitamos que el texto bloquee el ratón cuando aparece
            tooltipText.raycastTarget = false;
        }

        // ¡SEGURO ANTI-ERRORES! Si arrastraste la parada entera por error, lo bloqueamos
        if (tooltipObject == this.gameObject)
        {
            Debug.LogError($"¡Error en {gameObject.name}! Has arrastrado la parada entera al Tooltip Object. Debes arrastrar el HIJO vacío/panel que tiene el texto.", this);
            tooltipObject = null;
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
        // 3. Ocultamos SOLAMENTE el objeto del texto al empezar
        if (tooltipObject != null)
        {
            tooltipObject.SetActive(false);
        }
    }

    // --- FUNCIONES DEL RATÓN ---
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