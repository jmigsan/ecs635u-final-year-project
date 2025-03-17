using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class TESTNavMesh170325 : MonoBehaviour
{
    [Tooltip("Target transform the agent should navigate to")]
    public Transform target;

    private NavMeshAgent agent;
    
    void Start()
    {
        // Get the NavMeshAgent component
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        agent.SetDestination(target.position);
    }
}
