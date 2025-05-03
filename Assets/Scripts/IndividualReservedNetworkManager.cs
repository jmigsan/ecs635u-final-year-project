using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using NativeWebSocket;

public class IndividualReservedNetworkManager : MonoBehaviour
{
    public IndividualReservedGameManager individualReservedGameManager;

    WebSocket websocket;

    async void Start()
    {
        websocket = new WebSocket("ws://127.0.0.1:8002/ws/individual-reserved-brain");

        websocket.OnOpen += () =>
        {
            Debug.Log("Individual Reserved Brain connection open!");
        };

        websocket.OnClose += async (e) =>
        {
            Debug.Log("Individual Reserved Brain connection closed!");
        };

        websocket.OnError += (e) =>
        {
            Debug.Log("Individual Reserved Brain connection error!");
        };

        websocket.OnMessage += OnWebSocketMessage;

        InvokeRepeating("SendHeartbeat", 3.0f, 15.0f);

        await websocket.Connect();
    }

    public class ConversationMessage
    {
        public string type { get; set; } = "conversation";

        public string character { get; set; }
        public string message { get; set; }
        public string target { get; set; }
    }


    void OnWebSocketMessage(byte[] bytes)
    {
        string message = System.Text.Encoding.UTF8.GetString(bytes);
        JObject jsonObj = JObject.Parse(message);

        Debug.Log("Received message. JSON: " + message);

        string type = (string)jsonObj["type"];

        switch (type)
        {
            case "character_response":
                string characterResponseMessage = (string)jsonObj["message"];
                string target = (string)jsonObj["target"];
                individualReservedGameManager.MakeNpcTalk(target, characterResponseMessage);
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
        Debug.Log("Cafe couple director sent heartbeat. JSON: " + json);
    }

    async void OnApplicationQuit()
    {
        await websocket.Close();
    }

    public class CharacterProfile
    {
        public string name { get; set; }
        public int age { get; set; }
        public string occupation { get; set; }
        public string personality { get; set; }
        public string current_life_stage { get; set; }
        public string primary_goal { get; set; }
        public string backstory { get; set; }
        public string how_they_feel_about_current_life { get; set; }
    }

    public class InitialiseCharacterMessage
    {
        public string type { get; set; } = "initialise_character";
        public CharacterProfile character { get; set; }
    }

    public class PlayerInterruptionMessage
    {
        public string type { get; set; } = "player_interruption";
        public string message { get; set; }
    }

    public async void SendInitialiseCharacterMessage(IndividualReservedNpcController npc)
    {
        InitialiseCharacterMessage initialiseCharacterMessage = new InitialiseCharacterMessage
        {
            character = new CharacterProfile
            {
                name = npc.npcName,
                age = npc.age,
                occupation = npc.occupation,
                personality = npc.personality,
                current_life_stage = npc.currentLifeStage,
                primary_goal = npc.primaryGoal,
                backstory = npc.backstory,
                how_they_feel_about_current_life = npc.howTheyFeelAboutCurrentLife
            },
        };

        string json = JsonConvert.SerializeObject(initialiseCharacterMessage);

        await websocket.SendText(json);
        Debug.Log("Sent initialise character message. JSON: " + json);
    }

    public async void SendPlayerInterruptionMessage(string playerMessage)
    {
        PlayerInterruptionMessage playerInterruptionMessage = new PlayerInterruptionMessage
        {
            message = playerMessage
        };

        string json = JsonConvert.SerializeObject(playerInterruptionMessage);

        await websocket.SendText(json); 
        Debug.Log("Sent player interruption message. JSON: " + json);
    }
}
