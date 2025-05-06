using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using NativeWebSocket;

public class SceneDirectorNetworkManager : MonoBehaviour
{
    SceneDirector sceneDirector;
    WebSocket websocket;
    TaskCompletionSource<string> pendingSummaryRequest;

    void Awake()
    {
        sceneDirector = GetComponent<SceneDirector>();
    }

    async void Start()
    {
        websocket = new WebSocket("ws://127.0.0.1:8000/ws/scene-director");

        websocket.OnOpen += () =>
        {
            Debug.Log("scene-director connection open!");
        };

        websocket.OnClose += async (e) =>
        {
            Debug.Log("scene-director connection closed!");
        };

        websocket.OnError += (e) =>
        {
            Debug.Log("scene-director connection error!");
        };

        websocket.OnMessage += OnWebSocketMessage;

        InvokeRepeating("SendHeartbeat", 3.0f, 15.0f);

        await websocket.Connect();
    }

    public class Direction
    {
        public string character { get; set; }
        public string words { get; set; }
        public string target { get; set; }
        public string reasoning { get; set; }
    }

    void OnWebSocketMessage(byte[] bytes)
    {
        string message = System.Text.Encoding.UTF8.GetString(bytes);
        JObject jsonObj = JObject.Parse(message);

        Debug.Log("Received message. JSON: " + message);

        string type = (string)jsonObj["type"];

        switch (type)
        {
            case "directions":
                JArray directionArray = (JArray)jsonObj["directions"];
                List<Direction> directions = directionArray.ToObject<List<Direction>>();
                Debug.Log("directions received: " + JsonConvert.SerializeObject(directions));
    
                sceneDirector.LoadDirections(directions);
                break;

            case "direction_history_summary":
                Debug.Log("Received direction history summary.");
                string summary = (string)jsonObj["summary"];
            
                if (pendingSummaryRequest != null)
                {
                    pendingSummaryRequest.TrySetResult(summary);
                    pendingSummaryRequest = null;
                }
                break;

            case "heartbeat_ack":
                Debug.Log($"Received heartbeat ack.");
                break;

            default:
                Debug.LogWarning($"Unknown message type: {type}");
                break;
        }
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
        Debug.Log("Scene director sent heartbeat. JSON: " + json);
    }

    async void OnApplicationQuit()
    {
        await websocket.Close();
    }

    void OnDisable()
    {
        CancelInvoke("SendHeartbeat");
    }

    void OnDestroy()
    {
        CancelInvoke("SendHeartbeat");
    }

    public class Character
    {
        public string name { get; set; } 
        public int age { get; set; }
        public string gender { get; set; }
        public string occupation { get; set; }
        public string personality { get; set; }
        public string backstory { get; set; }
    }

    public class PreviousNpcConversations
    {
        public string character { get; set; }
        public string player_conversations { get; set; }
        public string group_conversations { get; set; }
    } 

    public class BeginSceneDirectionMessage
    {
        public string type { get; set; } = "begin_scene_direction";
        public List<Character> characters { get; set; }
        public string location { get; set; }
        public string time { get; set; }
        public string purpose { get; set; }
        public string player { get; set; }
        public string previously { get; set; }
        public string curriculum { get; set; }
    }

    public async Task SendBeginSceneDirection(List<Character> characters, string location, string time, string purpose, List<PreviousNpcConversations> previouslyData)
    {
        BeginSceneDirectionMessage message = new BeginSceneDirectionMessage();
        message.characters = characters;
        message.location = location;
        message.time = time;
        message.purpose = purpose;
        message.player = PlayerInfoManager.Instance.PlayerName;

        string previouslyJsonString = JsonConvert.SerializeObject(previouslyData);
        message.previously = previouslyJsonString;

        message.curriculum = PlayerInfoManager.Instance.GetCurriculum();

        string json = JsonConvert.SerializeObject(message);

        await websocket.SendText(json);
        Debug.Log("Scene director sent begin scene direction. JSON: " + json);
    }

    public class SendCompletedDirectionMessage
    {
        public string type { get; set; } = "completed_direction";
        public string character { get; set; }
        public string words { get; set; }
        public string target { get; set; }
    }

    public async void SendCompletedDirection(Direction direction)
    {
        SendCompletedDirectionMessage message = new SendCompletedDirectionMessage();
        message.character = direction.character;
        message.words = direction.words;
        message.target = direction.target;

        string json = JsonConvert.SerializeObject(message);

        await websocket.SendText(json);
        Debug.Log("Scene director sent completed direction. JSON: " + json);
    }

    public class PlayerInterruption
    {
        public string type { get; set; } = "player_interruption";
        public string action { get; set; }
        public string target { get; set; }
    }

    // Player input will directly call this function. This will stop the current conversation, add the player input to the conversation history, and the LLM will make a new conversation based on player input. 
    public async void SendPlayerInterruption(string action, string target)
    {
        Debug.LogAssertion("player_interruption was SENT!");

        PlayerInterruption message = new PlayerInterruption();
        message.action = action;
        message.target = target;

        string json = JsonConvert.SerializeObject(message);

        await websocket.SendText(json);
        Debug.Log("Scene director sent player interruption. JSON: " + json);
    }

    public class GetDirectionHistorySummary
    {
        public string type { get; set; } = "get_direction_history_summary";
    }

    public async Task<string> SendGetDirectionHistorySummary()
    {
        GetDirectionHistorySummary message = new GetDirectionHistorySummary();
        string json = JsonConvert.SerializeObject(message);
        Debug.Log("Scene director asked for scene summary. JSON: " + json);
        
        TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
        pendingSummaryRequest = tcs;
        
        await websocket.SendText(json);
        
        return await tcs.Task;
    }
}