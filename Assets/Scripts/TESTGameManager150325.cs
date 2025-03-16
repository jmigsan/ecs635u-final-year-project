using System.Collections.Generic;
using UnityEngine;

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

    public float characterPerceptibleRadius = 10f;
    public float characterActionableRadius = 3f;

    Dictionary<string, IActionable> actionables = new Dictionary<string, IActionable>();
    Dictionary<string, TESTNpcController150325> characters = new Dictionary<string, TESTNpcController150325>();

    void Start()
    {
        TESTNetworkManager140325.Instance.OnActionReceived += HandleCharacterAction;
        TESTNetworkManager140325.Instance.OnTalkActionReceived += HandleCharacterTalking;

        RegisterActionablesInScene();
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
                Debug.Log($"Found actionable: {actionable.entityName}");
            }
        }
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

        switch (actionInput)
        {
            case "walk":
                character.Walk(target);
                break;
        }
    }

    void HandleCharacterTalking(string characterInput, string actionInput, string targetInput, string messageInput)
    {
        TESTNpcController150325 character;
        TESTNpcController150325 target;

        character = characters[characterInput];
        target = characters[targetInput];

        character.Talk(target, messageInput);
    }

    public List<TESTNetworkManager140325.CharacterPerception> GetAllPerceptions()
    {
        List<TESTNetworkManager140325.CharacterPerception> perceptions = new List<TESTNetworkManager140325.CharacterPerception>();

        foreach (var characterEntry in characters)
        {
            List<IPerceptible> perceptibles = new List<IPerceptible>();
            List<IActionable> actionables = new List<IActionable>();

            GameObject characterGameObject = characterEntry.Value.gameObject;
            Vector3 position = characterGameObject.transform.position;

            Collider[] hitPerceptibleColliders = Physics.OverlapSphere(position, characterPerceptibleRadius);
            foreach (Collider hit in hitPerceptibleColliders)
            {
                IPerceptible perceptible = hit.gameObject.GetComponent<IPerceptible>();
                if (perceptible != null)
                {
                    perceptibles.Add(perceptible);
                    Debug.Log($"Inner sphere hit: {hit.name}, Perceptible name: {perceptible.entityName}, Type: {perceptible.type}");
                }
            }

            Collider[] hitActionableColliders = Physics.OverlapSphere(position, characterActionableRadius);
            foreach (Collider hit in hitActionableColliders)
            {
                IActionable actionable = hit.gameObject.GetComponent<IActionable>();
                if (actionable != null)
                {
                    actionables.Add(actionable);
                    Debug.Log($"Outer sphere hit: {hit.name}, Actionable name: {actionable.entityName}, Type: {actionable.type}, Action: {actionable.action}");
                }
            }

            List<TESTNetworkManager140325.ThingPerception> thingPerception = new List<TESTNetworkManager140325.ThingPerception>();
            foreach (IPerceptible perceptible in perceptibles)
            {
                TESTNetworkManager140325.ThingPerception thing = new TESTNetworkManager140325.ThingPerception();
                thing.type = perceptible.type;
                thing.entity = perceptible.entityName;
                thingPerception.Add(thing);
            }

            List<TESTNetworkManager140325.ActionPerception> actionPerception = new List<TESTNetworkManager140325.ActionPerception>();
            foreach (IActionable actionable in actionables)
            {
                TESTNetworkManager140325.ActionPerception act = new TESTNetworkManager140325.ActionPerception();
                act.target = actionable.entityName;
                act.action = actionable.action;
                actionPerception.Add(act);
            }

            TESTNetworkManager140325.CharacterPerception perception = new TESTNetworkManager140325.CharacterPerception();
            perception.character = characterEntry.Key;
            perception.thingsCharacterSees = thingPerception;
            perception.actionsCharacterCanDo = actionPerception;

            perceptions.Add(perception);
        }

        return perceptions;
    }

    public void SendCompletedAction(string type, string characterName, string action)
    {
        List<TESTNetworkManager140325.CharacterPerception> perceptions = GetAllPerceptions();
        TESTNetworkManager140325.Instance.SendCompletedAction(type, characterName, action, perceptions);
    }
}
