using UnityEngine;
using UnityEngine.Events;

public class ShopkeeperDoor : MonoBehaviour
{
    public ShopkeeperNpc shopkeeperNpc;
    public Vector3 doorForward = Vector3.forward;
    
    private Vector3 playerEntryPoint;
    private bool playerInTrigger;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerEntryPoint = other.transform.position;
            playerInTrigger = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && playerInTrigger)
        {
            playerInTrigger = false;
            Vector3 exitPoint = other.transform.position;
            Vector3 movementDirection = (exitPoint - playerEntryPoint).normalized;
            
            Vector3 worldDoorForward = transform.TransformDirection(doorForward.normalized);
            
            float dotProduct = Vector3.Dot(movementDirection, worldDoorForward);
            
            if (dotProduct > 0)
            {
                shopkeeperNpc.Door("exited");
                Debug.Log("Player exited");
            }
            else
            {
                shopkeeperNpc.Door("entered");
                Debug.Log("Player entered");
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.TransformDirection(doorForward.normalized) * 2);
    }
}
