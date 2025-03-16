using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class TESTNpcController150325 : MonoBehaviour
{
    public string characterName;
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

    public void Walk(IInteractable target)
    {
        navAgent.ResetPath();
        navAgent.SetDestination(target.GetDestination());        
    }

    private void StopWalking()
    {
        navAgent.ResetPath();
        SendCompletedAction("completed_direction", "walk");
    }

    public void Talk(TESTNpcController150325 target, string message)
    {
        Debug.Log($"{characterName} says to {target.characterName}: {message}");
        SendCompletedAction("completed_direction", "talk");
    }

    void SendCompletedAction(string type, string action)
    {
        TESTGameManager150325.Instance.SendCompletedAction(type, characterName, action);
    }

}
