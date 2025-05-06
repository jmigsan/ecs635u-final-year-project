// It has things to do
// It does them if it's not in a director scene

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine.Networking;
using TMPro;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class DirectableNpc : MonoBehaviour
{
    [System.Serializable]
    public class RoutineActivity
    {
        [Tooltip("Activity name and description are fed into LLM to tell it what to generate.")]
        public string activityName;
        [Tooltip("Activity name and description are fed into LLM to tell it what to generate.")]
        public string activityDescription;
        public int startHour;
        public int startMinute;
        public Transform location;
        public string animation;
    }

    [Header("Profile")]
    public string npcName;
    public int age;
    public string gender;
    public string occupation;
    public string personality;
    public string backstory;

    [Header("Settings")]
    public string voice = "en-GB-SoniaNeural";
    public float maxHearingDistance = 10f;

    [Header("Routine")]
    public List<RoutineActivity> routine = new List<RoutineActivity>();
    public bool inDirectorScene = false;
    public bool isAtDestination = false;
    public SceneDirector currentSceneDirector = null;

    RoutineActivity currentActivity = null;
    NavMeshAgent agent;
    // Animator animator;
    AudioSource audioSource;
    TextMeshPro subs;
    bool isTalking = false;
    Transform currentTalkTarget;
    bool playerIsTalking = false;
    DirectableNpcNetworkManager directableNpcNetworkManager;
    int lastTriggeredHour = -1;
    int lastTriggeredMinute = -1;
    List<RoutineActivity> completedActivities = new List<RoutineActivity>();
    
    void Start()
    {
        directableNpcNetworkManager = GetComponent<DirectableNpcNetworkManager>();
        agent = GetComponent<NavMeshAgent>();
        // animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        subs = transform.Find("Subs/Text").GetComponent<TextMeshPro>();
        subs.text = "";
        PlayerMicrophone.Instance.PlayerIsTalking += HandlePlayerIsTalking;
        SortRoutine();

        Invoke("SendInitialiseDirectable", 5f);
    }

    void OnDestroy()
    {
        PlayerMicrophone.Instance.PlayerIsTalking -= HandlePlayerIsTalking;
    }

    void HandlePlayerIsTalking(bool isTalking)
    {
        playerIsTalking = isTalking;

        if (!isTalking || audioSource == null || !audioSource.isPlaying)
        {
            return;
        }

        Transform playerTransform = PlayerInfoManager.Instance.GetTransform();
        float dist = Vector3.Distance(transform.position, playerTransform.position);

        if (dist <= maxHearingDistance)
        {
            audioSource.Stop();
            Debug.Log($"{npcName} TTS stopped because player started talking nearby");
        }
    }


    void Update()
    {
        CheckRoutine();
        RotateToTalkTarget();
    }

    void SortRoutine()
    {
        routine.Sort((activityA, activityB) =>
        {
            int hourComparison = activityA.startHour.CompareTo(activityB.startHour);

            if (hourComparison != 0)
            {
                return hourComparison;
            }

            return activityA.startMinute.CompareTo(activityB.startMinute);
        });

        Debug.Log($"{npcName}'s routine has been sorted chronologically.");
    }

    void CheckRoutine()
    {
        int gameHour = GameClock.Instance.hour;
        int gameMinute = GameClock.Instance.minute;

        if (gameHour == lastTriggeredHour && gameMinute == lastTriggeredMinute)
        {
            return;
        }
        
        lastTriggeredHour = gameHour;
        lastTriggeredMinute = gameMinute; 

        for (int i = 0; i < routine.Count; i++)
        {
            // Should be sorted, so the first one should be ok?
            if (gameHour >= routine[i].startHour && gameMinute >= routine[i].startMinute)
            {
                // if (currentActivity != null && !string.IsNullOrEmpty(currentActivity.animation))
                // {
                //     animator.SetBool(currentActivity.animation, false);
                // }

                if (routine[i] != currentActivity && !completedActivities.Contains(routine[i]))
                {
                    isAtDestination = false;
                    currentActivity = routine[i];
                    completedActivities.Add(routine[i]);
                    break;
                }
            }
        }

        // Director should hold these characters until their next routine activity. If it doesn't, then doesn't work.
        // I guess I could put buffer activities between that it goes to when director is finished? Find out later in testing.
        if (currentActivity != null && !inDirectorScene && !isAtDestination)
        {
            agent.SetDestination(currentActivity.location.position);
            // animator.SetBool("Walk", true);
            
            // Check if we've reached the destination
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                isAtDestination = true;
                // animator.SetBool("Walk", false);
                                
                // if (!string.IsNullOrEmpty(currentActivity.animation))
                // {
                //     animator.SetBool(currentActivity.animation, true);
                // }
            }
        }
    }

    void RotateToTalkTarget()
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

    public Transform GetTransform()
    {
        return transform;
    }

    public bool IsTalking()
    {
        return isTalking;
    }

    public void Listen(string message)
    {
        directableNpcNetworkManager.SendUserMessage(GameClock.Instance.GetTime(), message);
    }

    public void Talk(Transform target, string message)
    {
        Debug.Log($"{npcName} says: {message}");
        StartCoroutine(TalkWithTTS(target, message));
    }

    IEnumerator TalkWithTTS(Transform target, string message)
    {
        Debug.Log($"DN talk tts here, {target}, {message}");
        currentTalkTarget = target;
        subs.text = message;
        isTalking = true;

        yield return StartCoroutine(PlayTTS(message, voice));

        currentTalkTarget = null;
        subs.text = "";
        isTalking = false;
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

            if (playerIsTalking)
            {
                yield return null;
            }
        }
    }

    public void GiveSceneSummary(string summary)
    {
        directableNpcNetworkManager.SendScriptedConversation(summary);
    }

    public async Task<SceneDirectorNetworkManager.PreviousNpcConversations> GetAllConversationHistory()
    {
        TaskCompletionSource<SceneDirectorNetworkManager.PreviousNpcConversations> tcs = new TaskCompletionSource<SceneDirectorNetworkManager.PreviousNpcConversations>();
        directableNpcNetworkManager.SendGetAllConversationHistory(tcs);
        return await tcs.Task;
    }

    void SendInitialiseDirectable()
    {
        DirectableNpcNetworkManager.Character character = new DirectableNpcNetworkManager.Character
        {
            name = npcName,
            age = age,
            gender = gender,
            occupation = occupation,
            personality = personality,
            backstory = backstory
        };

        directableNpcNetworkManager.SendInitialiseDirectable(character, "Sorano Town", "You are a native to Sorano.");
    }

}