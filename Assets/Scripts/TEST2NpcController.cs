// NPCController.cs - Individual controller for each NPC
using UnityEngine;
using SocketIOUnity;
using System.Collections.Generic;
using Newtonsoft.Json;

public class TEST2NpcController : MonoBehaviour
{
    // NPC identity
    public string npcId;
    public string npcName;
    
    // Current state
    public string currentLocation;
    private List<string> nearbyObjects = new List<string>();
    private List<string> nearbyCharacters = new List<string>();
    
    // References to actual game objects (would be populated by the game)
    public List<GameObject> actualNearbyObjects = new List<GameObject>();
    public List<GameObject> actualNearbyCharacters = new List<GameObject>();
    
    // Socket reference - shared across all NPCs
    private static SocketIOUnity socket;
    private static string serverUrl = "http://localhost:3000";
    private static bool isInitialized = false;
    
    void Awake()
    {
        // Make sure we have a valid ID
        if (string.IsNullOrEmpty(npcId))
        {
            npcId = System.Guid.NewGuid().ToString();
            Debug.LogWarning($"NPC had no ID, generated: {npcId}");
        }
        
        // Initialize Socket.IO connection if not already initialized
        InitializeSocketIfNeeded();
    }
    
    void Start()
    {
        // Register this NPC's specific event handlers
        socket.On($"npc_command_{npcId}", HandleCommand);
        socket.On($"request_perception_{npcId}", (response) => {
            SendPerceptionUpdate();
        });
        
        // Initial perception update
        UpdatePerception();
        
        Debug.Log($"NPC initialized: {npcName} (ID: {npcId})");
    }
    
    private static void InitializeSocketIfNeeded()
    {
        if (!isInitialized)
        {
            socket = new SocketIOUnity(serverUrl, new SocketIOOptions());
            
            // Register global event handlers
            socket.On("connect", (response) => {
                Debug.Log("Connected to Python server");
            });
            
            socket.On("disconnect", (response) => {
                Debug.Log("Disconnected from Python server");
            });
            
            // Generic command handler that routes to specific NPCs
            socket.On("npc_command", (response) => {
                var data = response.GetValue<Dictionary<string, object>>();
                if (data.ContainsKey("npc_id"))
                {
                    string targetNpcId = data["npc_id"].ToString();
                    // Re-emit to the specific NPC channel
                    socket.Emit($"npc_command_{targetNpcId}", response.ToString());
                }
            });
            
            socket.On("request_perception", (response) => {
                var data = response.GetValue<Dictionary<string, object>>();
                if (data.ContainsKey("npc_id"))
                {
                    string targetNpcId = data["npc_id"].ToString();
                    // Re-emit to the specific NPC channel
                    socket.Emit($"request_perception_{targetNpcId}", response.ToString());
                }
            });
            
            // Connect to Socket.IO server
            socket.Connect();
            isInitialized = true;
        }
    }
    
    private void HandleCommand(SocketIOResponse response)
    {
        var data = response.GetValue<Dictionary<string, object>>();
        string action = data["action"].ToString();
        string target = data["target"].ToString();
        
        Debug.Log($"NPC {npcName} received command: {action} to {target}");
        
        switch (action)
        {
            case "walk":
                WalkTo(target);
                break;
            case "interact":
                InteractWith(target);
                break;
            case "speak":
                string message = data["message"].ToString();
                SpeakTo(target, message);
                break;
        }
        
        // Update and send perception after action
        UpdatePerception();
        SendPerceptionUpdate();
    }
    
    private void WalkTo(string target)
    {
        Debug.Log($"NPC {npcName} walking to {target}");
        
        // Update the current location based on target
        if (target == "player")
        {
            currentLocation = "near_player";
        }
        else if (nearbyObjects.Contains(target))
        {
            currentLocation = $"near_{target}";
        }
        else if (target == "village_center" || target == "town_gate" || target == "market")
        {
            currentLocation = target;
            SetPositionBasedOnLocation(target);
        }
    }
    
    private void SetPositionBasedOnLocation(string location)
    {
        // Simplified position setting
        Vector3 position = Vector3.zero;
        
        switch (location)
        {
            case "village_center":
                position = new Vector3(0, 0, 0);
                break;
            case "town_gate":
                position = new Vector3(10, 0, 10);
                break;
            case "market":
                position = new Vector3(-10, 0, 5);
                break;
            default:
                position = new Vector3(Random.Range(-20, 20), 0, Random.Range(-20, 20));
                break;
        }
        
        transform.position = position;
    }
    
    private void InteractWith(string target)
    {
        Debug.Log($"NPC {npcName} interacting with {target}");
        // Implement interaction logic
    }
    
    private void SpeakTo(string character, string message)
    {
        Debug.Log($"NPC {npcName} speaking to {character}: {message}");
        // Implement dialogue logic
    }
    
    private void UpdatePerception()
    {
        // Clear previous lists
        nearbyObjects.Clear();
        nearbyCharacters.Clear();
        
        // In a real game, use physics to detect nearby entities
        // For this example, we'll use location-based perception
        switch (currentLocation)
        {
            case "village_center":
                nearbyObjects.AddRange(new[] { "well", "bench", "market_stall" });
                nearbyCharacters.AddRange(new[] { "player", "villager2" });
                break;
            case "town_gate":
                nearbyObjects.AddRange(new[] { "gate", "guard_post", "torch" });
                nearbyCharacters.AddRange(new[] { "player", "merchant" });
                break;
            default:
                nearbyObjects.AddRange(new[] { "tree", "rock" });
                nearbyCharacters.Add("player");
                break;
        }
    }
    
    private void SendPerceptionUpdate()
    {
        // Create perception data to send back to Python
        var perceptionData = new Dictionary<string, object>
        {
            {"objects_you_can_walk_to", nearbyObjects},
            {"objects_you_can_interact_with", nearbyObjects},
            {"characters_you_can_walk_to", nearbyCharacters},
            {"characters_you_can_interact_with", nearbyCharacters},
            {"location", currentLocation},
            {"npc_id", npcId}
        };
        
        // Convert to JSON and send to Python
        string jsonData = JsonConvert.SerializeObject(perceptionData);
        socket.Emit("perception_update", jsonData);
        
        Debug.Log($"Sent perception update for NPC {npcName} to Python");
    }
}