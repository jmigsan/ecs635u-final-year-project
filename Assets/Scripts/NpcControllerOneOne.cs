// using System.Collections;
// using System.Text;
// using UnityEngine;
// using UnityEngine.Networking;

// public class NpcControllerOneOne : MonoBehaviour, IInteractable
// {
//     #region Websocket Code

//     // ------------- WEBSOCKET CODE -------------
//     class NPCMessage
//     {
//         public string type;
//         public string message;
//         public string from_npc;
//         public string target_npc;
//         public List<string> active_npcs;
//         public bool success;
//         public Dictionary<string, object> data;
//     }

//     WebSocket websocket;
//     [SerializeField] string serverUrl = "ws://localhost:8000/npc/";
//     [SerializeField] string npcId = "Avery";

//     Queue<string> messageQueue = new Queue<string>();
//     bool isConnected = false;
    
//     void Awake() // this dont need to be awake. it used to be start. i just didnt want it to mess with the other start() down below
//     {
//         ConnectToServer().ContinueWith(task => {
//             if (task.Exception != null) {
//                 Debug.LogError($"Connection failed: {task.Exception}");
//             }
//         }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
//     }

//     async System.Threading.Tasks.Task ConnectToServer()
//     {
//         websocket = new WebSocket($"{serverUrl}{npcId}");
        
//         websocket.OnOpen += () => {
//             isConnected = true;
//             Debug.Log($"NPC {npcId} connected to server");
//         };
        
//         websocket.OnError += (e) => {
//             isConnected = false;
//             Debug.LogError($"WebSocket Error: {e}");
//         };
        
//         websocket.OnClose += (e) => {
//             isConnected = false;
//             Debug.Log($"Connection closed: {e}");
//         };
        
//         websocket.OnMessage += (bytes) => {
//             string message = Encoding.UTF8.GetString(bytes);
//             messageQueue.Enqueue(message);
//         };

//         await websocket.Connect();
//     }

//     void Update()
//     {
//         ProcessMessageQueue();
//     }
    
//     void ProcessMessageQueue()
//     {
//         while (messageQueue.Count > 0)
//         {
//             string message = messageQueue.Dequeue();
//             Debug.Log($"Received message: {message}");

//             NPCMessage npcMessage = JsonUtility.FromJson<NPCMessage>(message);
//             HandleMessage(npcMessage);
//         }
//     }
    
//     private void HandleMessage(NPCMessage message)
//     {
//         switch (message.type)
//         {
//             case "npc_connected":
//             case "npc_disconnected":
//                 if (message.active_npcs != null)
//                 {
//                     activeNpcs = message.active_npcs;
//                 }
//                 break;
                
//             case "npc_message":
//                 Debug.Log($"Message from {message.from_npc}: {message.message}");
//                 // Handle NPC-to-NPC communication here
//                 // This is where you'd implement behavior based on the message
//                 break;
                
//             case "ack":
//             case "delivery_status":
//                 // Optional: handle acknowledgments if needed
//                 break;
                
//             default:
//                 Debug.Log($"Unknown message type: {message.type}");
//                 break;
//         }
//     }
    
//     // Public methods to interact with other NPCs
    
//     /// <summary>
//     /// Send a message to a specific NPC
//     /// </summary>
//     public async void SendMessageToNPC(string targetNpcId, string message, Dictionary<string, object> data = null)
//     {
//         if (!isConnected)
//         {
//             Debug.LogWarning("Not connected to server");
//             return;
//         }
        
//         Dictionary<string, object> messageData = new Dictionary<string, object>
//         {
//             { "target_npc", targetNpcId },
//             { "message", message }
//         };
        
//         if (data != null)
//         {
//             messageData["data"] = data;
//         }
        
//         await websocket.SendText(JsonUtility.ToJson(messageData));
//     }
    
//     private async void OnApplicationQuit()
//     {
//         if (websocket != null && websocket.State == WebSocketState.Open)
//         {
//             // Clean disconnect
//             await websocket.Close();
//         }
//     }
//     #endregion

//     #region Perception Code
//     // ------------------ PERCEPTION CODE -------------------

//     public LayerMask interactiveLayerMask;

//     Dictionary<GameObject, string> DetectInteractiveObjects(float detectionRadius)
//     {
//         Dictionary<GameObject, string> interactiveObjects = new Dictionary<GameObject, string>();
        
//         // Perform sphere cast to find all colliders within radius
//         Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius, interactiveLayerMask);
        
//         foreach (Collider collider in hitColliders)
//         {
//             GameObject obj = collider.gameObject;
            
//             // Check if object has an interactive component
//             IInteractable interactable = obj.GetComponent<IInteractable>();
            
//             if (interactable != null)
//             {
//                 // Get description from the interactable interface
//                 string description = interactable.GetDescription();
//                 interactiveObjects.Add(obj, description);
//             }
//         }
        
//         return interactiveObjects;
//     }
    
//     // This just tell you whats in its perception radius
//     void OnDrawGizmosSelected()
//     {
//         Gizmos.color = Color.yellow;
//         Gizmos.DrawWireSphere(transform.position, 10f);
        
//         Gizmos.color = Color.red;
//         Gizmos.DrawWireSphere(transform.position, 3f);
//     }
//     #endregion

//     #region Interaction Code
//     // -------------------- INTERACTION CODE ---------------------

//     NavMeshAgent navAgent;

//     void Start() // later put this up with the other start()
//     {
//         navAgent = GetComponent<NavMeshAgent>();
//     }

//     void Move(Vector3 destination)
//     {
//         navAgent.ResetPath();
//         navAgent.SetDestination(destination);
//     }

//     void Stop()
//     {
//         navAgent.ResetPath();
//     }

//     void Sit()
//     {
//         Stop();
//         Debug.Log("I'm Sitting!");
//     }

//     void Talk(string message)
//     {
//         Debug.Log($"I'm Talking: {message}");
//     }

//     void Idle()
//     {
//         Stop();
//         Debug.Log("I'm Idle");
//     }

//     void LookAt(Vector3 targetPosition)
//     {
//         Vector3 directionToTarget = targetPosition - transform.position;
//         directionToTarget.y = 0;
        
//         if (directionToTarget != Vector3.zero)
//         {
//             transform.rotation = Quaternion.LookRotation(directionToTarget);
//         }
//     }

//     void Perceive()
//     {
//         Dictionary<GameObject, string> walkableObjects = DetectInteractiveObjects(10f);
//         Dictionary<GameObject, string> interactableObjects = DetectInteractiveObjects(3f);
//         Debug.Log("Walkable Objects: " + walkableObjects)
//         Debug.Log("Interactable Objects: " + interactableObjects)
//     }
//     #endregion
// }