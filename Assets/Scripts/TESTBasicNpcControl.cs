using UnityEngine;
using UnityEngine.AI;

public class TESTBasicNpcControl : MonoBehaviour
{
    NavMeshAgent navAgent;

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
    }

    public void Move(Vector3 destination)
    {
        navAgent.ResetPath();
        navAgent.SetDestination(destination);
    }

    public void Stop()
    {
        navAgent.ResetPath();
    }

    public void Sit()
    {
        Stop();
        Debug.Log("I'm Sitting!");
    }

    public void Talk(string message)
    {
        Debug.Log($"I'm Talking: {message}");
    }

    public void Idle()
    {
        Stop();
        Debug.Log("I'm Idle");
    }

    public void LookAt(Vector3 targetPosition)
    {
        Vector3 directionToTarget = targetPosition - transform.position;
        directionToTarget.y = 0;
        
        if (directionToTarget != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(directionToTarget);
        }
    }
}
