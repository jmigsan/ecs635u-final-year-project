using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class NpcController : MonoBehaviour
{
    public class NpcQuery
    {
        public string query;
    }

    public class LlmResponse
    {
        public string llm_response;
    }

    void Start()
    {
        StartCoroutine(AskNpc("Hello, NPC!"));
    }

    public void Tell(string words)
    {
        StartCoroutine(AskNpc(words));
    }

    IEnumerator AskNpc(string words)
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
            string npcResponse = response.llm_response;
            Debug.Log("NPC Response: " + npcResponse);
        }
    }
}