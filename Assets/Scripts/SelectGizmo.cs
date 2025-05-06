using UnityEngine;

public class SelectGizmo : MonoBehaviour
{
    public Color gizmoColor = Color.yellow;
    public float gizmoSize = 1.0f;
    public bool drawWhenSelected = true;

    private void OnDrawGizmos()
    {
        if (!drawWhenSelected)
        {
            DrawGizmo();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (drawWhenSelected)
        {
            DrawGizmo();
        }
    }

    private void DrawGizmo()
    {
        Color previousColor = Gizmos.color;
        
        Gizmos.color = gizmoColor;
        
        Gizmos.DrawSphere(transform.position, gizmoSize);
        
        Gizmos.DrawLine(transform.position, transform.position + transform.right * gizmoSize);
        Gizmos.DrawLine(transform.position, transform.position + transform.up * gizmoSize);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * gizmoSize);
        
        Gizmos.color = previousColor;
    }
}
