using UnityEngine;

public class TESTSelfSphereGizmo170325 : MonoBehaviour
{
    [Tooltip("Color of the perceptible radius sphere")]
    public Color perceptibleSphereColor = Color.yellow;
    
    [Tooltip("Color of the actionable radius sphere")]
    public Color actionableSphereColor = Color.red;
    
    [Tooltip("Whether to draw the gizmos when the object is not selected")]
    public bool drawAlways = false;
    
    [Tooltip("Line width for the gizmo spheres")]
    [Range(1, 5)]
    public int lineWidth = 1;

    private void OnDrawGizmos()
    {
        if (drawAlways)
        {
            DrawSphereGizmos();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawAlways)
        {
            DrawSphereGizmos();
        }
    }

    private void DrawSphereGizmos()
    {
        // Get radius values from the GameManager
        float perceptibleRadius = 10f; // Default value
        float actionableRadius = 3f;   // Default value
        
        // Try to find the GameManager and get actual values
        if (TESTGameManager150325.Instance != null)
        {
            perceptibleRadius = TESTGameManager150325.Instance.characterPerceptibleRadius;
            actionableRadius = TESTGameManager150325.Instance.characterActionableRadius;
        }
        
        // Save the current Gizmos settings
        Color oldColor = Gizmos.color;
        
        // Draw perceptible radius sphere (larger)
        Gizmos.color = perceptibleSphereColor;
        DrawSphere(transform.position, perceptibleRadius);
        
        // Draw actionable radius sphere (smaller)
        Gizmos.color = actionableSphereColor;
        DrawSphere(transform.position, actionableRadius);
        
        // Restore original color
        Gizmos.color = oldColor;
    }
    
    private void DrawSphere(Vector3 center, float radius)
    {
        // Draw the main wire sphere
        Gizmos.DrawWireSphere(center, radius);
        
        // Optional: Draw additional spheres for thicker lines if lineWidth > 1
        if (lineWidth > 1)
        {
            float lineScale = 0.01f;
            for (int i = 1; i < lineWidth; i++)
            {
                Gizmos.DrawWireSphere(center, radius + (i * lineScale));
                Gizmos.DrawWireSphere(center, radius - (i * lineScale));
            }
        }
    }
}
