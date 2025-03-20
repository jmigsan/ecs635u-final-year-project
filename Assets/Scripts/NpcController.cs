using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using TMPro;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class NpcController : MonoBehaviour, IActionable, IPerceptible
{
    [Header("NPC Information")]
    [SerializeField] string _entityName;
    [SerializeField] string _description;
    [SerializeField] string _type;
    [SerializeField] List<string> _nearActions = new List<string> { "talk" };
    [SerializeField] List<string> _farActions = new List<string> { "walk" };
    

    [Header("Settings")]
    [SerializeField] float waitForPlayerTimeout = 5f;
    [SerializeField] string voice = "en-GB-SoniaNeural";

    public string entityName
    {
        get { return _entityName; }
        set { _entityName = value; }
    }

    public string description
    {
        get { return _description; }
        set { _description = value; }
    }

    public string type
    {
        get { return _type; }
        set { _type = value; }
    }

    public List<string> nearActions { get => _nearActions; set => _nearActions = value; }
    public List<string> farActions { get => _farActions; set => _farActions = value; }

    string currentWalkTarget = "";
    Transform currentTalkTarget = null;

    NavMeshAgent navAgent;

    AudioSource audioSource;
    
    TextMeshPro subs;


    bool playerIsTalking;

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        subs = transform.Find("Subs/Text").GetComponent<TextMeshPro>();

        PlayerMicrophone.Instance.PlayerIsTalking += HandlePlayerIsTalking;

        subs.text = "";
    }

    void HandlePlayerIsTalking(bool isTalking)
    {
        playerIsTalking = isTalking;

        if (isTalking && audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            
            Debug.Log("TTS audio stopped because player started talking");
        }
    }

    void Update()
    {
        if (currentTalkTarget != null)
        {
            RotateTowards(currentTalkTarget);
        }
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
        float stoppingDistance = navAgent.stoppingDistance + 2f; // Add a small buffer

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
        Debug.Log($"{entityName} says to {target.entityName}: {message}");
        StartCoroutine(TalkWithTTS(target, message));
    }

    IEnumerator TalkWithTTS(IPerceptible target, string message)
    {
        currentTalkTarget = target.GetTransform();
        subs.text = message;

        yield return StartCoroutine(PlayTTS(message, voice));

        currentTalkTarget = null;
        subs.text = "";

        if (!playerIsTalking)
        {
            // This only sends that the character said the thing only if the player didn't interrupt them. To make sure the player actually heard them and that they're contributing. 
            SendCompletedAction("completed_direction", "talk", target.entityName, message);
        }
    }

    public class TtsQuery
    {
        public string words;
        public string voice;
    }

    IEnumerator PlayTTS(string message, string voice)
    {
        TtsQuery query = new TtsQuery { words = message, voice = voice };
        string jsonQuery = JsonUtility.ToJson(query);

        using (UnityWebRequest www = new UnityWebRequest("http://127.0.0.1:8000/tts", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonQuery);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerAudioClip(www.url, AudioType.MPEG);
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error: {www.error}");
                yield break;
            }

            AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
            if (audioClip == null)
            {
                Debug.LogError("Failed to download audio clip");
                yield break;
            }

            if (audioSource == null)
            {
                Debug.LogError("AudioSource is not initialized");
                yield break;
            }

            audioSource.clip = audioClip;
            audioSource.Play();

            while (audioSource.isPlaying && !playerIsTalking)
            {
                yield return null;
            }
            
            if (playerIsTalking && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }


    public void WaitForPlayer(IPerceptible target)
    {
        StartCoroutine(WaitForPlayerWithDelay(target));
    }

    IEnumerator WaitForPlayerWithDelay(IPerceptible target)
    {
        currentTalkTarget = target.GetTransform();

        float elapsedTime = 0f;

        while (elapsedTime < waitForPlayerTimeout && !playerIsTalking)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (!playerIsTalking)
        {
            SendCompletedAction("player_interruption", "player_silence", target.entityName);
        }
        else
        {
            Debug.Log($"Player started talking before timeout, not sending player_silence action");
        }

        currentTalkTarget = null;
    }


    void SendCompletedAction(string type, string action, string target, string message = "")
    {
        Debug.Log($"Sent a completed action of type: {type}, action: {action}, target: {target}, message: {message}");

        if (string.IsNullOrEmpty(message))
        {
            GameManager.Instance.SendCompletedAction(type, entityName, action, target);
        }
        else
        {
            GameManager.Instance.SendCompletedAction(type, entityName, action, target, message);
        }
    }
}
