using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class NpcController : MonoBehaviour
{
    public void Tell(string words)
    {
        Debug.Log("You told NPC: " + words.Trim());
        if (words.Trim() != "")
        {
            StartCoroutine(TellCoroutineChain(words));
        }

    }

    class NpcResponse
    {
        public string Words { get; set; }
    }

    IEnumerator TellCoroutineChain(string words)
    {
        NpcResponse npcResponse = new NpcResponse();

        Debug.Log("Starting coroutine 1");
        yield return StartCoroutine(AskNpc(words.Trim(), npcResponse));
        Debug.Log("Starting coroutine 2");
        yield return StartCoroutine(PlayTTS(npcResponse));
    }

    public class NpcQuery
    {
        public string query;
    }

    public class LlmResponse
    {
        public string llm_response;
    }

    IEnumerator AskNpc(string words, NpcResponse npcResponse)
    {
        NpcQuery query = new NpcQuery { query = words };
        string jsonQuery = JsonUtility.ToJson(query);

        using (UnityWebRequest www = new UnityWebRequest("http://127.0.0.1:8000/llm", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonQuery);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            string jsonResponse = www.downloadHandler.text;
            LlmResponse response = JsonUtility.FromJson<LlmResponse>(jsonResponse);
            Debug.Log("NPC Response: " + response.llm_response);

            npcResponse.Words = response.llm_response;
        }
    }

    public class TtsQuery
    {
        public string words;
    }

    IEnumerator PlayTTS(NpcResponse npcResponse)
    {
        TtsQuery query = new TtsQuery { words = npcResponse.Words };
        string jsonQuery = JsonUtility.ToJson(query);

        using (UnityWebRequest www = new UnityWebRequest("http://127.0.0.1:8000/tts", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonQuery);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerAudioClip(www.url, AudioType.MPEG);
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            AudioSource audioSource = GetComponent<AudioSource>();
            AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
            audioSource.clip = audioClip;
            audioSource.Play();
        }
    }
}