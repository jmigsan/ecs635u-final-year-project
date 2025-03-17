using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class TESTNpcController150325 : MonoBehaviour, IActionable, IPerceptible
{
    [Header("NPC Information")]
    [SerializeField] string _entityName;
    [SerializeField] string _description;
    [SerializeField] string _type;
    [SerializeField] List<string> _nearActions = new List<string> {"talk"};
    [SerializeField] List<string> _farActions = new List<string> {"walk"};

    public string entityName {
        get { return _entityName; }
        set { _entityName = value; }
        }

    public string description {
        get { return _description; }
        set { _description = value; }
        }

    public string type {
        get { return _type; }
        set { _type = value; }
        }

    public List<string> nearActions { get => _nearActions; set => _nearActions = value; }
    public List<string> farActions { get => _farActions; set => _farActions = value; }

    string currentWalkTarget = "";
    Transform currentTalkTarget = null;

    NavMeshAgent navAgent;

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
    }

    // void Update()
    // {
    //     // I'll need this later
    //     // if (isInConversation && currentTalkTarget != null)
    //     // {
    //     //     RotateTowards(currentTalkTarget);
    //     // }
    // }

    Vector3 IPerceptible.GetPosition()
    {
        return transform.position;
    }

    Transform IPerceptible.GetTransform()
    {
        return transform;
    }

    public void Walk(IPerceptible target)
    {
        Debug.Log($"Starting walk from {transform.position} to {target.GetPosition()}");
        StartCoroutine(WalkRoutine(target));
    }

    IEnumerator WalkRoutine(IPerceptible target)
    {
        Debug.Log("Walk coroutine started");
        
        // Check if target is null
        if (target == null)
        {
            Debug.LogError("Walk target is null!");
            yield break;
        }
        
        // Check if navAgent is null
        if (navAgent == null)
        {
            navAgent = GetComponent<NavMeshAgent>();
            if (navAgent == null)
            {
                Debug.LogError("NavMeshAgent component not found!");
                yield break;
            }
        }
        
        currentWalkTarget = target.entityName;
        navAgent.ResetPath();
        
        // Get target position safely
        Vector3 targetPosition;
        try
        {
            targetPosition = target.GetPosition();
            Debug.Log($"Target position: {targetPosition}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error getting target position: {ex.Message}");
            StopWalking();
            yield break;
        }
        
        // Request the path
        navAgent.SetDestination(targetPosition);
        
        // Wait for path calculation to complete
        yield return new WaitUntil(() => !navAgent.pathPending);
        
        Debug.Log($"Path calculation complete. hasPath: {navAgent.hasPath}, pathStatus: {navAgent.pathStatus}");
        
        // Check if a valid path was found
        if (!navAgent.hasPath || navAgent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            Debug.LogError($"Could not find a valid path to {target.entityName} at {targetPosition}");
            StopWalking();
            yield break;
        }
        
        float timeout = 20f;
        float elapsedTime = 0f;
        float stoppingDistance = navAgent.stoppingDistance + 2.5f; // Add a small buffer
        
        while (elapsedTime < timeout)
        {
            // Get current positions - target might move
            Vector3 currentPosition = transform.position;
            Vector3 currentTargetPos = target.GetPosition();
            
            // Calculate 2D distance (ignore Y axis differences)
            Vector2 currentPos2D = new Vector2(currentPosition.x, currentPosition.z);
            Vector2 targetPos2D = new Vector2(currentTargetPos.x, currentTargetPos.z);
            float flatDistance = Vector2.Distance(currentPos2D, targetPos2D);
            
            // Debug.Log($"Walking...FlatDistance: {flatDistance}, StoppingDistance: {stoppingDistance}, RemainingDistance: {navAgent.remainingDistance}");
            
            // Check if we're close enough using horizontal distance
            if (flatDistance <= stoppingDistance)
            {
                Debug.Log("Destination reached (close enough) - ending walk");
                break;
            }
            
            // Also check if NavMeshAgent thinks we've arrived
            if (!navAgent.pathPending && navAgent.remainingDistance <= stoppingDistance)
            {
                Debug.Log("Destination reached (agent report) - ending walk");
                break;
            }
            
            // The path might be lost during movement
            if (!navAgent.hasPath && !navAgent.pathPending)
            {
                Debug.Log("Path lost during walking");
                break;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        if (elapsedTime >= timeout)
        {
            Debug.Log($"Walking to {target.entityName} timed out after {timeout} seconds");
        }
        
        Debug.Log("Walk coroutine completed, now calling StopWalking()");
        StopWalking();
    }


    void StopWalking()
    {
        Debug.Log($"{entityName} stopped walking to {currentWalkTarget}");
        navAgent.ResetPath();
        SendCompletedAction("completed_direction", "walk", currentWalkTarget);
        currentWalkTarget = "";
    }

    public void Talk(IPerceptible target, string message)
    {
        StartCoroutine(TalkWithDelayForTesting(target, message));
    }

    IEnumerator TalkWithDelayForTesting(IPerceptible target, string message)
    {
        yield return new WaitForSeconds(4f);

        currentTalkTarget = target.GetTransform();
        
        RotateTowards(currentTalkTarget);

        Debug.Log($"{entityName} says to {target.entityName}: {message}");

        currentTalkTarget = null;

        SendCompletedAction("completed_direction", "talk", target.entityName, message);
    }

    void RotateTowards(Transform target)
    {
        if (target == null) return;
        
        Vector3 direction = target.position - transform.position;
        direction.y = 0;
        
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    void SendCompletedAction(string type, string action, string target, string message = "")
    {
        Debug.Log($"Sent a completed action of type: {type}, action: {action}, target: {target}, message: {message}");

        if (string.IsNullOrEmpty(message))
        {
            TESTGameManager150325.Instance.SendCompletedAction(type, entityName, action, target);
        }
        else
        {
            TESTGameManager150325.Instance.SendCompletedAction(type, entityName, action, target, message);
        }
    }
}
