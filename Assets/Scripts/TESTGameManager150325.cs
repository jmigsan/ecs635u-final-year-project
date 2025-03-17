using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class TESTGameManager150325 : MonoBehaviour
{
    public static TESTGameManager150325 Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public TESTNpcController150325 haruto;
    public TESTNpcController150325 aiko;
    public TESTNpcController150325 sakura;
    public GameObject player;

    public float characterPerceptibleRadius = 10f;
    public float characterActionableRadius = 3f;

    Dictionary<string, IActionable> actionables = new Dictionary<string, IActionable>();
    Dictionary<string, IPerceptible> perceptibles = new Dictionary<string, IPerceptible>();
    Dictionary<string, TESTNpcController150325> characters = new Dictionary<string, TESTNpcController150325>();

    void Start()
    {
        TESTNetworkManager140325.Instance.OnActionReceived += HandleCharacterAction;
        TESTNetworkManager140325.Instance.OnTalkActionReceived += HandleCharacterTalking;
        TESTNetworkManager140325.Instance.OnWalkActionReceived += HandleCharacterWalking;

        RegisterActionablesInScene();
        RegisterPerceptiblesInScene();
        RegisterCharacterControllers();
    }

    void RegisterActionablesInScene()
    {
        MonoBehaviour[] allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        foreach (MonoBehaviour mb in allMonoBehaviours)
        {
            if (mb is IActionable actionable)
            {
                actionables[actionable.entityName] = actionable;
            }
        }

        Debug.Log("Actionables: " + string.Join(", ", actionables.Keys));
    }

    void RegisterPerceptiblesInScene()
    {
        MonoBehaviour[] allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        foreach (MonoBehaviour mb in allMonoBehaviours)
        {
            if (mb is IPerceptible perceptible)
            {
                perceptibles[perceptible.entityName] = perceptible;
            }
        }

        Debug.Log("Perceptibles: " + string.Join(", ", perceptibles.Keys));
    }

    void RegisterCharacterControllers()
    {
        characters["Haruto"] = haruto;
        characters["Aiko"] = aiko;
        characters["Sakura"] = sakura;
    }

    void HandleCharacterAction(string characterInput, string actionInput, string targetInput)
    {
        TESTNpcController150325 character;
        IActionable target;

        character = characters[characterInput];
        target = actionables[targetInput];

        Debug.Log("Rip. Nothing should be here. This should never run.");

        // if i code any actions, like pick up or something, it would go here
        // like a switch case statement
        // e.g.
        // switch(target)
        //      case "pick up":
        //          character.PickUp(target);
        //          break;

    }

    void HandleCharacterWalking(string characterInput, string actionInput, string targetInput)
    {
        TESTNpcController150325 character;
        IPerceptible target;
        
        character = characters[characterInput];
        target = perceptibles[targetInput];

        Debug.Log($"{characterInput} will {actionInput} to {target.entityName} at {target.GetPosition()}");

        character.Walk(target);
    }

    void HandleCharacterTalking(string characterInput, string actionInput, string targetInput, string messageInput)
    {
        TESTNpcController150325 character;
        IPerceptible target;

        character = characters[characterInput];
        target = perceptibles[targetInput];

        character.Talk(target, messageInput);
    }

    public List<TESTNetworkManager140325.CharacterPerception> GetAllPerceptions()
    {
        List<TESTNetworkManager140325.CharacterPerception> perceptions = new List<TESTNetworkManager140325.CharacterPerception>();

        foreach (var characterEntry in characters)
        {
            List<IPerceptible> perceptibles = new List<IPerceptible>();

            List<IActionable> nearActionables = new List<IActionable>();
            List<IActionable> farActionables = new List<IActionable>();
            Dictionary<string, TESTNetworkManager140325.ActionPerception> actionDictionary = new Dictionary<string, TESTNetworkManager140325.ActionPerception>();

            GameObject characterGameObject = characterEntry.Value.gameObject;
            Vector3 position = characterGameObject.transform.position;

            // Things it can see
            Collider[] hitPerceptibleColliders = Physics.OverlapSphere(position, characterPerceptibleRadius);
            foreach (Collider hit in hitPerceptibleColliders)
            {
                IPerceptible perceptible = hit.gameObject.GetComponent<IPerceptible>();
                if (perceptible != null && perceptible.entityName != characterEntry.Key) // Exclude self
                {
                    perceptibles.Add(perceptible);
                    // Debug.Log($"Outer sphere hit: {hit.name}, Perceptible name: {perceptible.entityName}, Type: {perceptible.type}");
                }
            }

            // Near actions
            Collider[] hitNearActionableColliders = Physics.OverlapSphere(position, characterActionableRadius);
            foreach (Collider hit in hitNearActionableColliders)
            {
                
                IActionable actionable = hit.gameObject.GetComponent<IActionable>();
                if (actionable != null && actionable.entityName != characterEntry.Key)
                {
                    nearActionables.Add(actionable);
                    // Debug.Log($"Inner sphere hit: {hit.name}, Actionable name: {actionable.entityName}, Type: {actionable.type}, Action: {actionable.nearActions}");
                }
            }

            // Far actions
            Collider[] hitFarActionableColliders = Physics.OverlapSphere(position, characterPerceptibleRadius);
            foreach (Collider hit in hitFarActionableColliders)
            {
                IActionable actionable = hit.gameObject.GetComponent<IActionable>();
                if (actionable != null && actionable.entityName != characterEntry.Key)
                {
                    farActionables.Add(actionable);
                    // Debug.Log($"Outer sphere hit: {hit.name}, Actionable name: {actionable.entityName}, Type: {actionable.type}, Action: {actionable.farActions}");
                }
            }

            List<TESTNetworkManager140325.ThingPerception> thingPerception = new List<TESTNetworkManager140325.ThingPerception>();
            List<TESTNetworkManager140325.ActionPerception> actionPerception = new List<TESTNetworkManager140325.ActionPerception>();

            // This gets everything perceptible and sends it back
            foreach (IPerceptible perceptible in perceptibles)
            {
                TESTNetworkManager140325.ThingPerception thing = new TESTNetworkManager140325.ThingPerception();
                thing.type = perceptible.type;
                thing.entity = perceptible.entityName;
                thing.description = perceptible.description;
                thingPerception.Add(thing);
            }

            foreach (IActionable nearActionable in nearActionables)
            {
                string entityName = nearActionable.entityName;
                
                if (actionDictionary.ContainsKey(entityName))
                {
                    List<string> existingActions = actionDictionary[entityName].actions;
                    
                    if (nearActionable.nearActions != null)
                    {
                        foreach (string action in nearActionable.nearActions)
                        {
                            if (!existingActions.Contains(action))
                            {
                                existingActions.Add(action);
                            }
                        }
                    }
                }
                else
                {
                    TESTNetworkManager140325.ActionPerception act = new TESTNetworkManager140325.ActionPerception();
                    act.target = entityName;
                    
                    act.actions = new List<string>();
                    
                    if (nearActionable.nearActions != null)
                    {
                        act.actions.AddRange(nearActionable.nearActions);
                    }
                    
                    actionDictionary.Add(entityName, act);
                }
            }
            
            foreach (IActionable farActionable in farActionables)
            {
                string entityName = farActionable.entityName;
                
                if (actionDictionary.ContainsKey(entityName))
                {
                    List<string> existingActions = actionDictionary[entityName].actions;
                    
                    if (farActionable.farActions != null)
                    {
                        foreach (string action in farActionable.farActions)
                        {
                            if (!existingActions.Contains(action))
                            {
                                existingActions.Add(action);
                            }
                        }
                    }
                }
                else
                {
                    TESTNetworkManager140325.ActionPerception act = new TESTNetworkManager140325.ActionPerception();
                    act.target = entityName;
                    
                    act.actions = new List<string>();
                    
                    if (farActionable.farActions != null)
                    {
                        act.actions.AddRange(farActionable.farActions);
                    }
                    
                    actionDictionary.Add(entityName, act);
                }
            }

            actionPerception = new List<TESTNetworkManager140325.ActionPerception>(actionDictionary.Values);

            TESTNetworkManager140325.CharacterPerception perception = new TESTNetworkManager140325.CharacterPerception();
            perception.character = characterEntry.Key;
            perception.thingsCharacterSees = thingPerception;
            perception.actionsCharacterCanDo = actionPerception;

            perceptions.Add(perception);
        }

        Debug.Log("Perceptions: " + JsonConvert.SerializeObject(perceptions, Formatting.Indented));
        return perceptions;
    }

    public void SendCompletedAction(string type, string characterName, string action, string target, string message = "")
    {
        List<TESTNetworkManager140325.CharacterPerception> perceptions = GetAllPerceptions();

        if (string.IsNullOrEmpty(message))
        {
            TESTNetworkManager140325.Instance.SendCompletedAction(type, characterName, action, target, perceptions);
        }
        else
        {
            TESTNetworkManager140325.Instance.SendCompletedAction(type, characterName, action, target, perceptions, message);
        }
    }
}
