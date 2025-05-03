using UnityEngine;
using System;
using System.Collections;
using UnityEngine.AI;
using UnityEngine.Networking;
using TMPro;

public class ShopkeeperNpc : MonoBehaviour
{
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

    NavMeshAgent agent;
    AudioSource audioSource;
    TextMeshPro subs;
    bool isTalking = false;
    Transform currentTalkTarget;
    bool playerIsTalking;
    ShopkeeperNetworkManager shopkeeperNetworkManager;

    void Start()
    {
        shopkeeperNetworkManager = GetComponent<ShopkeeperNetworkManager>();
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        subs = transform.Find("Subs/Text").GetComponent<TextMeshPro>();
        subs.text = "";
        PlayerMicrophone.Instance.PlayerIsTalking += HandlePlayerIsTalking;
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
        RotateToTalkTarget();
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

    public void Listen(string message)
    {
        shopkeeperNetworkManager.SendUserMessage(GameClock.Instance.GetTime(), message);
    }

    public void Talk(Transform target, string message)
    {
        Debug.Log($"{npcName} says: {message}");
        StartCoroutine(TalkWithTTS(target, message));
    }

    public void Door(string action)
    {
        shopkeeperNetworkManager.SendDoorMessage(GameClock.Instance.GetTime(), action);
    }

    IEnumerator TalkWithTTS(Transform target, string message)
    {
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

        using (UnityWebRequest www = new UnityWebRequest("http://127.0.0.1:8001/tts", "POST"))
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
}