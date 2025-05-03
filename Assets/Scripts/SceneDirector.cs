using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;

public class SceneDirector : MonoBehaviour
{
    public int requiredCharacterCount = 3;
    public List<DirectableNpc> npcsInArea = new List<DirectableNpc>();
    public string location = "Sorano";

    public class ScheduleEntry
    {
        public string time;
        public string purpose;
    }

    public List<ScheduleEntry> schedule = new List<ScheduleEntry>
    {
        new ScheduleEntry {time = "08:00", purpose = "Calm, peaceful, natural conversation between friends."}
    };

    SceneDirectorNetworkManager sceneDirectorNetworkManager;
    bool directing = false;
    Dictionary<string, DirectableNpc> npcs = new Dictionary<string, DirectableNpc>();

    void Awake()
    {
        sceneDirectorNetworkManager = GetComponent<SceneDirectorNetworkManager>();
    }

    void OnTriggerEnter(Collider other)
    {
        DirectableNpc npc = other.GetComponent<DirectableNpc>();
        
        if (npc != null && !npcsInArea.Contains(npc))
        {
            npcsInArea.Add(npc);
        }
    }

    void OnTriggerExit(Collider other)
    {
        DirectableNpc npc = other.GetComponent<DirectableNpc>();
        
        if (npc != null && npcsInArea.Contains(npc))
        {
            npcsInArea.Remove(npc);
        }
    }

    void Update()
    {
        if (!directing && npcsInArea.Count >= requiredCharacterCount)
        {
            bool allNpcsAreReady = true;
            foreach (DirectableNpc npc in npcsInArea)
            {
                if (npc.isAtDestination == false)
                {
                    allNpcsAreReady = false;
                    break;
                }
            }
            
            if (allNpcsAreReady)
            {
                directing = true;
                _ = StartDirectionAsync();
            }
        }
    }

    public async Task StartDirectionAsync()
    {
        Debug.Log("Initialising characters for scene directions");
        npcs.Clear();

        foreach (DirectableNpc npc in npcsInArea)
        {
            npc.inDirectorScene = true;
            npcs[npc.npcName] = npc;
            npc.currentSceneDirector = this;
        }

        // Tell LLM all character names, profiles, activity names, activity description.
        // returns steps for these npcs (to network manager)
        // do the directions (network manager tell this the directions)

        List<SceneDirectorNetworkManager.Character> characters = new List<SceneDirectorNetworkManager.Character>();
        List<SceneDirectorNetworkManager.PreviousNpcConversations> previousConversations = new List<SceneDirectorNetworkManager.PreviousNpcConversations>();

        foreach (DirectableNpc npc in npcsInArea)
        {
            SceneDirectorNetworkManager.Character character = new SceneDirectorNetworkManager.Character
            {
                name = npc.npcName,
                age = npc.age,
                gender = npc.gender,
                occupation = npc.occupation,
                personality = npc.personality,
                backstory = npc.backstory
            };
            
            characters.Add(character);

            SceneDirectorNetworkManager.PreviousNpcConversations allPreviousConversations = await npc.GetAllConversationHistory();

            SceneDirectorNetworkManager.PreviousNpcConversations previous = new SceneDirectorNetworkManager.PreviousNpcConversations
            {
                character = npc.npcName,
                player_conversations = allPreviousConversations.player_conversations,
                group_conversations = allPreviousConversations.group_conversations
            };

            previousConversations.Add(previous);
        }

        string time = $"{GameClock.Instance.hour:D2}:{GameClock.Instance.minute:D2}";

        schedule.Sort((a, b) => string.Compare(a.time, b.time));

        ScheduleEntry currentPurpose = null;
        foreach (ScheduleEntry entry in schedule)
        {
            if (string.Compare(entry.time, time) <= 0)
            {
                currentPurpose = entry;
            }
            else
            {
                break;
            }
        }

        if (currentPurpose == null)
        {
            currentPurpose = schedule[0];
        }

        string purpose = currentPurpose.purpose;

        try
        {
            await sceneDirectorNetworkManager.SendBeginSceneDirection(characters, location, time, purpose, previousConversations);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during StartDirection: {ex.Message}");
            directing = false;
        }
    }

    public void LoadDirections(List<SceneDirectorNetworkManager.Direction> directions)
    {
        Debug.Log($"Received directions: {string.Join(", ", directions)}");
        StartCoroutine(ProcessDirections(directions));
    }

    IEnumerator ProcessDirections(List<SceneDirectorNetworkManager.Direction> directions)
    {
        foreach (SceneDirectorNetworkManager.Direction direction in directions)
        {
            if(direction.character == "System")
            {
                if(direction.words == "Conversation Complete")
                {
                    ReleaseNpcs();
                    break;
                }
            }

            else if(npcs.ContainsKey(direction.character))
            {
                DirectableNpc npc = npcs[direction.character];
                
                Transform target = this.transform;

                if (npcs.ContainsKey(direction.target))
                {
                    target = npcs[direction.target].GetTransform();
                }
                else if (direction.target == PlayerInfoManager.Instance.PlayerName)
                {
                    target = PlayerInfoManager.Instance.GetTransform();
                }
                else
                {
                    Debug.LogError($"Target {direction.target} not found.");
                }

                npc.Talk(target, direction.words);

                while (npc.IsTalking())
                {
                    yield return null;
                }

                sceneDirectorNetworkManager.SendCompletedDirection(direction);
            }
            else
            {
                Debug.LogError($"Character {direction.character} not found.");
            }
        }
    }

    public async void ReleaseNpcs()
    {
        Debug.Log("Releasing all NPCs from SceneDirector");

        string summary = await sceneDirectorNetworkManager.SendGetDirectionHistorySummary();

        foreach (DirectableNpc npc in npcsInArea)
        {
            npc.inDirectorScene = false;
            npc.currentSceneDirector = null;
            npc.GiveSceneSummary(summary);
        }

        npcs.Clear();

        directing = false;
    }

    public void SendPlayerInterruption(string action, string target)
    {
        sceneDirectorNetworkManager.SendPlayerInterruption(action, target);
    }
}