using UnityEngine;

public class SubsRotate : MonoBehaviour
{
    Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        Vector3 directionToCamera = mainCamera.transform.position - transform.position;
        directionToCamera.y = 0; // Lock Y-axis rotation
        
        if (directionToCamera != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(-directionToCamera);
        }
    }
}