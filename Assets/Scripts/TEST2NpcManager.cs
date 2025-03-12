// // NPCManager.cs - Manages NPC creation and access
// using UnityEngine;
// using System.Collections.Generic;

// public class TEST2NpcManager : MonoBehaviour
// {
//     [System.Serializable]
//     public class NPCDefinition
//     {
//         public string id;
//         public string name;
//         public string initialLocation;
//         public string personality;
//     }
    
//     // NPC prefab with TEST2NpcController attached
//     public GameObject npcPrefab;
    
//     // List of NPCs to create at startup
//     public List<NPCDefinition> npcDefinitions = new List<NPCDefinition>();
    
//     // Dictionary to track created NPCs
//     private Dictionary<string, TEST2NpcController> npcControllers = new Dictionary<string, TEST2NpcController>();
    
//     void Start()
//     {
//         CreateInitialNPCs();
//     }
    
//     private void CreateInitialNPCs()
//     {
//         foreach (var def in npcDefinitions)
//         {
//             CreateNPC(def.id, def.name, def.initialLocation);
//         }
//     }
    
//     public TEST2NpcController CreateNPC(string id, string name, string initialLocation)
//     {
//         // Instantiate NPC from prefab
//         GameObject npcObject = Instantiate(npcPrefab, Vector3.zero, Quaternion.identity);
//         npcObject.name = name;
        
//         // Get and configure controller
//         TEST2NpcController controller = npcObject.GetComponent<TEST2NpcController>();
//         controller.npcId = id;
//         controller.npcName = name;
//         controller.currentLocation = initialLocation;
        
//         // Store reference
//         npcControllers[id] = controller;
        
//         return controller;
//     }
    
//     public TEST2NpcController GetNPC(string id)
//     {
//         if (npcControllers.ContainsKey(id))
//         {
//             return npcControllers[id];
//         }
//         return null;
//     }
// }