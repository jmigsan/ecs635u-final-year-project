using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using NativeWebSocket;

public class FollowerNpcNetworkManager : MonoBehaviour
{
    FollowerNpc followerNpc;
    WebSocket websocket;
    void Awake()
    {
        followerNpc = GetComponent<FollowerNpc>();   
    }

    async void Start()
    {
        websocket = new WebSocket("ws://127.0.0.1:8003/ws/follower-npc");

        websocket.OnOpen += () =>
        {
            Debug.Log("follower-npc connection open!");
        };

        websocket.OnClose += async (e) =>
        {
            Debug.Log("follower-npc connection closed!");
        };

        websocket.OnError += (e) =>
        {
            Debug.Log("follower-npc connection error!");
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
                followerNpc.Talk(PlayerInfoManager.Instance.GetTransform(), (string)jsonObj["message"]);
                break;

            case "first_conversation":
                followerNpc.Talk(PlayerInfoManager.Instance.GetTransform(), (string)jsonObj["message"]);
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
        Debug.Log("FollowerNpc sent heartbeat. JSON: " + json);
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

    public class InitialiseFollower
    {
        public string type { get; set; } = "initialise_follower";
        public Character character { get; set; }
        public string location { get; set; }
        public string knowledge { get; set; }
        public string curriculum { get; set; }
        public string player { get; set; }
        public string language { get; set; }
        public string proficiency { get; set; }
        public string native { get; set; }
    }

    public async void SendInitialiseFollower(Character character, string location, string knowledge)
    {
        InitialiseFollower message = new InitialiseFollower();
        message.character = character;
        message.location = location;
        message.knowledge = knowledge;
        message.curriculum = PlayerInfoManager.Instance.GetCurriculum();
        message.player = PlayerInfoManager.Instance.PlayerName;
        message.language = PlayerInfoManager.Instance.Language;
        message.proficiency = PlayerInfoManager.Instance.Proficiency;
        message.native = PlayerInfoManager.Instance.Native;

        string json = JsonConvert.SerializeObject(message);

        await websocket.SendText(json);
        Debug.Log("Follower " + character.name + " sent initialise. JSON: " + json);
    }

    public class UserMessage
    {
        public string type { get; set; } = "user_message";
        public string player { get; set; }
        public string time { get; set; }
        public string message { get; set; }
    }

    public async void SendUserMessage(string words)
    {
        UserMessage message = new UserMessage();
        message.player = PlayerInfoManager.Instance.PlayerName;
        message.time = GameClock.Instance.GetTime();
        message.message = words;

        string json = JsonConvert.SerializeObject(message);

        await websocket.SendText(json);
        Debug.Log("User message sent to directable. JSON: " + json);
    }

    public class FirstConversationMessage
    {
        public string type { get; set; } = "first_conversation";
    }

    public async void SendFirstConversation()
    {
        FirstConversationMessage message = new FirstConversationMessage();
        string json = JsonConvert.SerializeObject(message);
        await websocket.SendText(json);
    }
}