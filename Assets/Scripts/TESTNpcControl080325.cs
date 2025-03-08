using UnityEngine;
using SocketIOUnity;

public class TESTNpcControl080325 : MonoBehaviour
{
    public string npcName;

    static SocketIOUnity socket;
    static string serverUrl = "http://localhost:3000";
    static bool isInitialized = false;

    void Awake()
    {
        InitializeSocket();
    }

    void Start()
    {
        Dictionary<string, string> npcData = new Dictionary<string, string>
        {
            {"npc_name", npcName}
        };
        
        // Convert to JSON and send to Python
        string initialiseNpcData = JsonConvert.SerializeObject(perception);
        socket.Emit("initialise_npc", initialiseNpcData)

        // Register this NPC's specific event handlers
        socket.On($"npc_command_{npcName}", HandleCommand);
        socket.On($"request_perception_{npcName}", (response) => {
            SendPerceptionUpdate();
        });
        
        // Initial perception update
        // UpdatePerception();
        
        Debug.Log($"NPC initialized: {npcName} (ID: {npcId})");
    }

    static void InitializeSocket()
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
                Dictionary<string, object> data = response.GetValue<Dictionary<string, object>>();

                string targetNpcName = data["npc_name"].ToString();
                socket.Emit($"npc_command_{targetNpcName}", response.ToString());
            });
            
            socket.On("get_perception", (response) => {
                Dictionary<string, object> data = response.GetValue<Dictionary<string, object>>();

                string targetNpcName = data["npc_name"].ToString();
                socket.Emit($"get_perception_{targetNpcName}", response.ToString());
            });
            
            // Connect to Socket.IO server
            socket.Connect();
            isInitialized = true;
        }
    }

    void HandleCommand(SocketIOResponse response)
    {
        <Dictionary<string, object>> command = response.GetValue<Dictionary<string, object>>();
        string action = command["action"].ToString();
        string target = command["target"].ToString();

        if (action == "walk")
        {
            Walk(target);
        }

        if (action == "interact")
        {
            Interact(target);
        }

        if (action == "speak")
        {
            string message = command["message"].ToString();
            Speak(target, message);
        }
    }

    void Walk(string target)
    {
        Debug.Log($"{npcName} is told to walk to {target}")
    }

    void Interact(string target)
    {
        Debug.Log($"{npcName} is told to interact with {target}")

    }

    void Speak(string target, string message)
    {
        Debug.Log($"{npcName} is told to speak to {target} and say {message}")
    }

    void SendPerceptionUpdate()
    {
        Dictionary<string, object> perception = new Dictionary<string, object>
        {
            {"objects_you_can_walk_to", ["apple", "pear", "banana", "orange"]},
            {"objects_you_can_interact_with", ["dog", "cat", "bird", "fish"]},
            {"characters_you_can_walk_to", ["red", "blue", "green", "yellow"]},
            {"characters_you_can_interact_with", ["car", "truck", "bus", "train"]},
            {"location", "sup"},
            {"npc_name", npcName}
        };
        
        // Convert to JSON and send to Python
        string jsonData = JsonConvert.SerializeObject(perception);
        socket.Emit("perception_update", jsonData);
        Debug.Log("Sent perception update to python")
    }
}