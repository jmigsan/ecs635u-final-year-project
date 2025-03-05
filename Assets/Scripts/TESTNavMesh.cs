using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class TESTNavMesh : MonoBehaviour
{
    NavMeshAgent agent;
    InputAction moveAction;
    InputAction returnAction;
    Vector3 startingPosition;

    public Transform target;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();  
        startingPosition = transform.position;  

        XRInputManager.Instance.XButtonPressed += MoveToTarget;
        XRInputManager.Instance.YButtonPressed += ReturnToStart;

        moveAction = new InputAction("Move", binding: "<Keyboard>/m");
        moveAction.performed += ctx => MoveToTarget(true); 
        moveAction.Enable();  

        returnAction = new InputAction("Return", binding: "<Keyboard>/n");
        returnAction.performed += ctx => ReturnToStart(true);
        returnAction.Enable();
    }

    void MoveToTarget(bool isPressed)
    {
        // Only set destination when the button is pressed
        agent.destination = target.position;
    }

    void ReturnToStart(bool isPressed)
    {
        agent.destination = startingPosition;
    }
}
