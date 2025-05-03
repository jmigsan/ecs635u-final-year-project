using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using NativeWebSocket;

public class ShopkeeperNetworkManager : MonoBehaviour
{
    ShopkeeperNpc shopkeeperNpc;
    WebSocket websocket;

    void Awake()
    {
        shopkeeperNpc = GetComponent<ShopkeeperNpc>();
    }

    async void Start()
    {
        websocket = new WebSocket("ws://127.0.0.1:8001/ws/shopkeeper-npc");

        websocket.OnOpen += () =>
        {
            Debug.Log("shopkeeper-npc connection open!");
        };

        websocket.OnClose += async (e) =>
        {
            Debug.Log("shopkeeper-npc connection closed!");
        };

        websocket.OnError += (e) =>
        {
            Debug.Log("shopkeeper-npc connection error!");
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
                shopkeeperNpc.Talk(PlayerInfoManager.Instance.GetTransform(), (string)jsonObj["response"]);
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

    public class InitialiseShopkeeper
    {
        public string type { get; set; } = "initialise_shopkeeper";
        public Character character { get; set; }
        public string location { get; set; }
        public string knowledge { get; set; }
        public string curriculum { get; set; }
    }

    public async void SendInitialiseShopkeeper(Character shopkeeper, string location, string knowledge)
    {
        InitialiseShopkeeper message = new InitialiseShopkeeper();
        message.character = shopkeeper;
        message.location = location;
        message.knowledge = knowledge;
        message.curriculum = PlayerInfoManager.Instance.GetCurriculum();

        string json = JsonConvert.SerializeObject(message);

        await websocket.SendText(json);
        Debug.Log("Shopkeeper sent initialise. JSON: " + json);
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
        Debug.Log("User message sent to shopkeeper. JSON: " + json);
    }

    public class DoorMessage
    {
        public string type { get; set; } = "door_message";
        public string time { get; set; }
        public string action { get; set; } //Can either be 'entered' or 'exited'.
    }

    public async void SendDoorMessage(string time, string action)
    {
        DoorMessage message = new DoorMessage();
        message.time = time;
        message.action = action;

        string json = JsonConvert.SerializeObject(message);

        await websocket.SendText(json);
        Debug.Log("Door message sent to shopkeeper. JSON: " + json);
    }
}