using UnityEngine;
using System.Collections.Generic;

public class RailNode : MonoBehaviour
{
    [Tooltip("Drag the nodes you can travel to from this point")]
    public List<RailNode> connections;

    private void Awake()
    {
        foreach (RailNode node in connections)
        {
            if (node != null && !node.connections.Contains(this))
            {
                node.connections.Add(this);
            }
        }
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