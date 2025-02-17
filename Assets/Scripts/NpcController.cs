using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class NpcController : MonoBehaviour
{
    public string npcName;
    public string npcCharacterProfile;

    LayerMask npcLayerMask;

    void Start()
    {
        npcLayerMask = LayerMask.GetMask("NPC");
    }

    public void Tell(string words)
    {
        Debug.Log("NPC has been told: " + words.Trim());
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

    public class LlmQuery
    {
        public string query;
    }

    public class LlmResponse
    {
        public string response;
    }

    public class AskLlmResponse
    {
        public string response;
    }

    IEnumerator AskLlm(string words, AskLlmResponse askLlmResponse)
    {
        LlmQuery query = new LlmQuery { query = words };
        string jsonQuery = JsonUtility.ToJson(query);

        using (UnityWebRequest www = new UnityWebRequest("http://127.0.0.1:8000/llm", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonQuery);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            string jsonResponse = www.downloadHandler.text;
            LlmResponse llmResponse = JsonUtility.FromJson<LlmResponse>(jsonResponse);
            Debug.Log("LLM Response: " + llmResponse.response);

            askLlmResponse.response = llmResponse.response;
        }
    }

    IEnumerator AskNpc(string words, NpcResponse npcResponse)
    {
        AskLlmResponse askLlmResponse = new AskLlmResponse();
        yield return StartCoroutine(AskLlm(words, askLlmResponse));

        npcResponse.Words = askLlmResponse.response;
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

    void FixedUpdate() 
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, 10, npcLayerMask))
        { 
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
            BumpedIntoAnNpc(hit.collider);
        }
        else
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 10, Color.white);
        }
    }

    void BumpedIntoAnNpc(Collider other)
    {
        if (GetInstanceID() < other.GetInstanceID())
        {
            return;
        }

        NpcController otherNpc = other.GetComponent<NpcController>();

        string npcConversationHistory = "Sure. Let's go get some coffee.";
        string npcLocation = "Bad Moon Coffee Shop"; //Replace this with something that gets set in the Start() that checks the scene name or a scene manager or something.
        string npcThoughtProcessPrompt = $"You are {npcName}. This is your character profile: {npcCharacterProfile}. This is past your past conversation history {npcConversationHistory}. You are in {npcLocation}. You are talking to {otherNpc.npcName}. This is their character profile: {otherNpc.npcCharacterProfile}. Say something appropriate, natural, with respect to your previous conversations, and your goal as a character.";
        
        StartCoroutine(TalkToNpcThisBumpedInto(otherNpc, npcThoughtProcessPrompt));
    }

    IEnumerator TalkToNpcThisBumpedInto(NpcController otherNpc, string npcThoughtProcessPrompt)
    {
        AskLlmResponse askLlmResponse = new AskLlmResponse();
        yield return StartCoroutine(AskLlm(npcThoughtProcessPrompt, askLlmResponse));

        otherNpc.Tell(askLlmResponse.response);
    }
}