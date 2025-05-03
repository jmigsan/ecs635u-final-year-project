using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using NativeWebSocket;

public class DirectableNpcNetworkManager : MonoBehaviour
{
    DirectableNpc directableNpc;
    WebSocket websocket;
    TaskCompletionSource<SceneDirectorNetworkManager.PreviousNpcConversations> pendingConversationHistoryRequest;

    void Awake()
    {
        directableNpc = GetComponent<DirectableNpc>();
    }

    async void Start()
    {
        websocket = new WebSocket("ws://127.0.0.1:8002/ws/directable-npc");

        websocket.OnOpen += () =>
        {
            Debug.Log("directable-npc connection open!");
        };

        websocket.OnClose += async (e) =>
        {
            Debug.Log("directable-npc connection closed!");
        };

        websocket.OnError += (e) =>
        {
            Debug.Log("directable-npc connection error!");
        };

        websocket.OnMessage += OnWebSocketMessage;

        InvokeRepeating("SendHeartbeat", 3.0f, 15.0f);

        await websocket.Connect();
    }

    void OnWebSocketMessage(byte[] bytes)
    {
        string message = System.Text.Encoding.UTF8.GetString(bytes);
        JObject jsonObj = JObject.Parse(message);

        Debug.Log("Received message. JSON: " + message);

        string type = (string)jsonObj["type"];

        switch (type)
        {
            case "response":
                directableNpc.Talk(PlayerInfoManager.Instance.GetTransform(), (string)jsonObj["message"]);
                break;

            case "all_conversation_history":
                string playerConvoJson = jsonObj["conversation_history"]?.ToString(Newtonsoft.Json.Formatting.None);
                string groupConvoJson = jsonObj["scripted_conversation_history"]?.ToString(Newtonsoft.Json.Formatting.None);

                SceneDirectorNetworkManager.PreviousNpcConversations conversationHistory = new SceneDirectorNetworkManager.PreviousNpcConversations {
                    character = directableNpc.npcName,
                    player_conversations = playerConvoJson,
                    group_conversations = groupConvoJson
                };
                
                if (pendingConversationHistoryRequest != null)
                {
                    pendingConversationHistoryRequest.TrySetResult(conversationHistory);
                    pendingConversationHistoryRequest = null;
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
        Debug.Log("directable npc sent heartbeat. JSON: " + json);
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

    public class InitialiseDirectable
    {
        public string type { get; set; } = "initialise_directable";
        public Character character { get; set; }
        public string location { get; set; }
        public string knowledge { get; set; }
        public string curriculum { get; set; }
    }

    public async void SendInitialiseDirectable(Character character, string location, string knowledge)
    {
        InitialiseDirectable message = new InitialiseDirectable();
        message.character = character;
        message.location = location;
        message.knowledge = knowledge;
        message.curriculum = PlayerInfoManager.Instance.GetCurriculum();

        string json = JsonConvert.SerializeObject(message);

        await websocket.SendText(json);
        Debug.Log("Directable " + character.name + " sent initialise. JSON: " + json);
    }

    public class UserMessage
    {
        public string type { get; set; } = "user_message";
        public string player { get; set; }
        public string time { get; set; }
        public string message { get; set; }
    }

    public async void SendUserMessage(string time, string words)
    {
        UserMessage message = new UserMessage();
        message.player = PlayerInfoManager.Instance.PlayerName;
        message.time = time;
        message.message = words;

        string json = JsonConvert.SerializeObject(message);

        await websocket.SendText(json);
        Debug.Log("User message sent to directable. JSON: " + json);
    }

    public class ScriptedConversationMessage
    {
        public string type { get; set; } = "scripted_conversation";
        public string time { get; set; }
        public string summary { get; set; }
    }

    public async void SendScriptedConversation(string summary)
    {
        ScriptedConversationMessage message = new ScriptedConversationMessage();
        message.time = GameClock.Instance.GetTime();
        message.summary = summary;

        string json = JsonConvert.SerializeObject(message);

        await websocket.SendText(json);
        Debug.Log("Scripted conversation sent to directable. JSON: " + json);
    }

    public class GetAllConversationHistoryMessage
    {
        public string type { get; set; } = "get_all_conversation_history";
    }

    public async void SendGetAllConversationHistory(TaskCompletionSource<SceneDirectorNetworkManager.PreviousNpcConversations> tcs)
    {
        pendingConversationHistoryRequest = tcs;
        GetAllConversationHistoryMessage message = new GetAllConversationHistoryMessage();
        string json = JsonConvert.SerializeObject(message);
        await websocket.SendText(json);
        Debug.Log("Directable sent get all conversation history. JSON: " + json);
    }
}