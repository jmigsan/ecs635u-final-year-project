using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using TMPro;

public class CafeCoupleNpcController : MonoBehaviour
{
    [Header("NPC Information")]
    public string npcName;
    public int age;
    public string occupation;
    public string personality;
    public string currentLifeStage;
    public string primaryGoal;
    public string backstory;
    public string howTheyFeelAboutCurrentLife;

    [Header("Settings")]
    public string voice = "en-GB-SoniaNeural";

    AudioSource audioSource;

    TextMeshPro subs;

    bool isTalking = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        subs = transform.Find("Subs/Text").GetComponent<TextMeshPro>();

        subs.text = "";
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

    Transform GetTransform()
    {
        return transform;
    }

    public bool IsTalking()
    {
        return isTalking;
    }

    public void Talk(CafeCoupleNpcController target, string message)
    {
        Debug.Log($"{npcName} says to {target.npcName}: {message}");
        StartCoroutine(TalkWithTTS(target, message));
    }

    IEnumerator TalkWithTTS(CafeCoupleNpcController target, string message)
    {
        currentTalkTarget = target.GetTransform();
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

            while (audioSource.isPlaying)
            {
                yield return null;
            }

            audioSource.Stop();
        }
    }
}
