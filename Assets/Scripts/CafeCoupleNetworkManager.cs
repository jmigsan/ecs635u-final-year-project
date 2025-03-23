using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using NativeWebSocket;

public class CafeCoupleNetworkManager : MonoBehaviour
{
    public cafeCoupleGameManager cafeCoupleGameManager;

    WebSocket websocket;

    async void Start()
    {
        websocket = new WebSocket("ws://127.0.0.1:8001/ws/cafe-couple-director");

        websocket.OnOpen += () =>
        {
            Debug.Log("cafe-couple-director connection open!");
        };

        websocket.OnClose += async (e) =>
        {
            Debug.Log("cafe-couple-director connection closed!");
        };

        websocket.OnError += (e) =>
        {
            Debug.Log("cafe-couple-director connection error!");
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
            case "conversation":
                List<ConversationMessage> conversation = (List<ConversationMessage>)jsonObj["conversation"];
                cafeCoupleGameManager.LoadConversation(conversation);
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

    public class BeginCoupleConversationMessage
    {
        public string type { get; set; } = "begin_couple_conversation";

        [JsonProperty("character_1")]
        public CharacterProfile character1 { get; set; }

        [JsonProperty("character_2")]
        public CharacterProfile character2 { get; set; }

        public string relationship { get; set; }
        public string tone { get; set; }
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

    public class ContinueCoupleConversationMessage
    {
        public string type { get; set; } = "continue_couple_conversation";

        [JsonProperty("character_1")]
        public CharacterProfile character1 { get; set; }

        [JsonProperty("character_2")]
        public CharacterProfile character2 { get; set; }

        public string relationship { get; set; }
        public string tone { get; set; }
    }

    public async void SendBeginConversation(CafeCoupleNpcController char1, CafeCoupleNpcController char2, string relationship, string tone)
    {
        BeginCoupleConversationMessage beginCoupleConversationMessage = new BeginCoupleConversationMessage
        {
            character1 = new CharacterProfile
            {
                name = char1.npcName,
                age = char1.age,
                occupation = char1.occupation,
                personality = char1.personality,
                current_life_stage = char1.currentLifeStage,
                primary_goal = char1.primaryGoal,
                backstory = char1.backstory,
                how_they_feel_about_current_life = char1.howTheyFeelAboutCurrentLife
            },
            character2 = new CharacterProfile
            {
                name = char2.npcName,
                age = char2.age,
                occupation = char2.occupation,
                personality = char2.personality,
                current_life_stage = char2.currentLifeStage,
                primary_goal = char2.primaryGoal,
                backstory = char2.backstory,
                how_they_feel_about_current_life = char2.howTheyFeelAboutCurrentLife
            },
            relationship = relationship
            tone = tone
        };

        string json = JsonConvert.SerializeObject(beginCoupleConversationMessage);

        await websocket.SendText(json);
        Debug.Log("Sent begin couple conversation. JSON: " + json);
    }

    public async void SendContinueConversation(CafeCoupleNpcController char1, CafeCoupleNpcController char2, string relationship, string tone)
    {
        ContinueCoupleConversationMessage continueCoupleConversationMessage = new ContinueCoupleConversationMessage
        {
            character1 = new CharacterProfile
            {
                name = char1.npcName,
                age = char1.age,
                occupation = char1.occupation,
                personality = char1.personality,
                current_life_stage = char1.currentLifeStage,
                primary_goal = char1.primaryGoal,
                backstory = char1.backstory,
                how_they_feel_about_current_life = char1.howTheyFeelAboutCurrentLife
            },
            character2 = new CharacterProfile
            {
                name = char2.npcName,
                age = char2.age,
                occupation = char2.occupation,
                personality = char2.personality,
                current_life_stage = char2.currentLifeStage,
                primary_goal = char2.primaryGoal,
                backstory = char2.backstory,
                how_they_feel_about_current_life = char2.howTheyFeelAboutCurrentLife
            },

            relationship = relationship,
            tone = tone,
        };

        string json = JsonConvert.SerializeObject(continueCoupleConversationMessage);

        await websocket.SendText(json); 
        Debug.Log("Sent continue couple conversation. JSON: " + json);
    }
}
