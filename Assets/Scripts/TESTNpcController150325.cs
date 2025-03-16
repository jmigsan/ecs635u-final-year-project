using UnityEngine;
using UnityEngine.AI;

public class TESTNpcController150325 : MonoBehaviour, IPerceptible
{
    public string entityName { get; set; }
    public string type { get; set; }

    NavMeshAgent navAgent;

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (navAgent.hasPath && navAgent.remainingDistance <= navAgent.stoppingDistance && !navAgent.pathPending)
        {
            StopWalking();
        }
    }

    Vector3 IPerceptible.GetPosition()
    {
        return transform.position;
    }

    public void Walk(IActionable target)
    {
        navAgent.ResetPath();
        navAgent.SetDestination(target.GetPosition());
    }

    private void StopWalking()
    {
        navAgent.ResetPath();
        SendCompletedAction("completed_direction", "walk");
    }

    public void Talk(TESTNpcController150325 target, string message)
    {
        Debug.Log($"{entityName} says to {target.entityName}: {message}");
        SendCompletedAction("completed_direction", "talk");
    }

    void SendCompletedAction(string type, string action)
    {
        TESTGameManager150325.Instance.SendCompletedAction(type, entityName, action);
    }
}
