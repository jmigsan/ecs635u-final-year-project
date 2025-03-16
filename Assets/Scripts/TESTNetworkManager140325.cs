using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using NativeWebSocket;

public class TESTNetworkManager140325 : MonoBehaviour
{
    public static TESTNetworkManager140325 Instance { get; private set; }
    WebSocket websocket;

    public event Action<string, string, string> OnActionReceived;
    public event Action<string, string, string, string> OnTalkActionReceived;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        websocket = new WebSocket("ws://127.0.0.1:8000/narrative-engine");

        websocket.OnOpen += () =>
        {
            Debug.Log("Connection open!");
        };

        websocket.OnClose += async (e) =>
        {
            Debug.Log("Connection closed! Reconnecting in 2 seconds...");
            await Task.Delay(2000);
            await websocket.Connect();
        };

        websocket.OnError += (e) =>
        {
            Debug.Log("Connection error!");
        };

        websocket.OnMessage += OnWebSocketMessage;

        InvokeRepeating("SendHeartbeat", 3.0f, 15.0f);

        await websocket.Connect();
    }

    public class CharacterPerception
    {
        public string character { get; set; }

        [JsonProperty("things_character_sees")]
        public List<ThingPerception> thingsCharacterSees { get; set; } = new List<ThingPerception>();

        [JsonProperty("actions_character_can_do")]
        public List<ActionPerception> actionsCharacterCanDo { get; set; } = new List<ActionPerception>();
    }

    public class ThingPerception
    {
        public string type { get; set; }  // "character" or "object"
        public string entity { get; set; }
    }

    public class ActionPerception
    {
        public string target { get; set; }
        public string action { get; set; }
    }

    // things character sees
    // [
    //      {"type": "character", "entity": "Aiko"}
    //      {"type": "object", "entity": "chair"}
    // ]

    // actions characters can do
    // [
    //      {"target": "chair", "action": "sit"}
    //      {"target": "coffee", "action": "pick up"}
    //      {"target": "Aiko", "action": "talk"}
    //      {"target": "Haruto", "action": "wave"}
    // ]

    public class CompletedAction
    {
        public string type { get; set; } // Either "completed_direction" or "player_interruption"
        public string time { get; set; }
        public string character { get; set; }
        public string action { get; set; }

        [JsonProperty("character_perceptions")]
        public List<CharacterPerception> characterPerceptions { get; set; } = new List<CharacterPerception>();
    }

    void OnWebSocketMessage(byte[] bytes)
    {
        string message = System.Text.Encoding.UTF8.GetString(bytes);
        JObject jsonObj = JObject.Parse(message);

        string type = (string)jsonObj["type"];

        switch (type)
        {
            case "director_response":
                string character = (string)jsonObj["character"];
                string action = (string)jsonObj["action"];
                string target = (string)jsonObj["target"];

                if (action == "talk")
                {
                    string messageText = (string)jsonObj["message"];
                    Debug.Log($"{character} will talk to {target} saying: {messageText}");

                    OnTalkActionReceived?.Invoke(character, action, target, messageText);
                }
                else
                {
                    Debug.Log($"{character} will {action} {target}");

                    OnActionReceived?.Invoke(character, action, target);
                }
                break;

            case "heartbeat_ack":
                string timestamp = (string)jsonObj["timestamp"];
                Debug.Log($"Received heartbeat ack. Timestamp: {timestamp}");
                break;

            default:
                Debug.LogWarning($"Unknown message type: {type}");
                break;
        }
    }

    public async void SendCompletedAction(
        string type,
        string character,
        string action,
        List<CharacterPerception> perceptions)
    {
        long unixTimestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        string time = unixTimestamp.ToString();

        CompletedAction completedAction = new CompletedAction
        {
            type = type,
            time = time,
            character = character,
            action = action,
            characterPerceptions = perceptions
        };

        string json = JsonConvert.SerializeObject(completedAction);
        await websocket.SendText(json);
        Debug.Log("Sent completed action. JSON: " + json);
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket.DispatchMessageQueue();
#endif
    }

    class HeartbeatMessage
    {
        public string type { get; set; } = "heartbeat";
    }

    async void SendHeartbeat()
    {
        HeartbeatMessage message = new HeartbeatMessage();
        string json = JsonConvert.SerializeObject(message);
        await websocket.SendText(json);
        Debug.Log("Sent heartbeat. JSON: " + json);
    }

    async void OnApplicationQuit()
    {
        await websocket.Close();
    }

    public class BeginStoryMessage
    {
        public string type { get; set; } = "begin_story";

        [JsonProperty("character_perceptions")]
        public List<CharacterPerception> characterPerceptions { get; set; } = new List<CharacterPerception>();
    }

    async void SendBeginStory()
    {
        BeginStoryMessage beginStoryMessage = new BeginStoryMessage
        {
            characterPerceptions = TESTGameManager150325.Instance.GetAllPerceptions()
        };

        string json = JsonConvert.SerializeObject(beginStoryMessage);

        await websocket.SendText(json);
        Debug.Log("Sent begin story. JSON: " + json);
    }

}
