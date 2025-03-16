using System.Collections;
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

    Dictionary<string, IInteractable> interactables = new Dictionary<string, IInteractable>();
    Dictionary<string, TESTNpcController150325> characters = new Dictionary<string, TESTNpcController150325>();

    void Start()
    {
        TESTNetworkManager140325.Instance.OnActionReceived += HandleCharacterAction;
        TESTNetworkManager140325.Instance.OnTalkActionReceived += HandleCharacterTalking;

        RegisterInteractablesInScene();
        RegisterCharacterControllers();
    }

    void RegisterInteractablesInScene()
    {
        MonoBehaviour[] allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();

        foreach (MonoBehaviour mb in allMonoBehaviours)
        {
            if (mb is IInteractable interactable)
            {
                interactables[interactable.interactableName] = interactable;
                Debug.Log($"Found interactable: {interactable.interactableName}");
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
        IInteractable target;

        character = characters[characterInput];
        target = interactables[targetInput];

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

    public void SendCompletedAction(string type, string characterName, string action)
    {
        List<TESTNetworkManager140325.CharacterPerception> perceptions = new List<TESTNetworkManager140325.CharacterPerception>();

        foreach (var characterEntry in characters)
        {
            GameObject characterGameObject = characterEntry.Value.gameObject;

            List<TESTNetworkManager140325.CharacterPerception> characterPerception = new List<TESTNetworkManager140325.CharacterPerception>();

            TESTNetworkManager140325.CharacterPerception perception = new TESTNetworkManager140325.CharacterPerception();
            perception.character = characterEntry.Key;

            Vector3 position = characterGameObject.transform.position;
            Collider[] hitColliders = Physics.OverlapSphere(position, radius);

            List<TestNetworkManager140325.ThingPerception> thingPerception = new List<TestNetworkManager140325.ThingPerception>();


            List<TestNetworkManager140325.ActionPerception> actionPerception = new List<TestNetworkManager140325.ActionPerception>();


        }

        TESTNetworkManager140325.Instance.SendCompletedAction(type, characterName, action, perceptions);
    }
}
