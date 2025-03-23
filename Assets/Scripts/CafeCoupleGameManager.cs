using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using UnityEngine.Random;

public class CafeCoupleGameManager : MonoBehaviour
{
    public CafeCoupleNetworkManager cafeCoupleNetworkManager;

    public CafeCoupleNpcController character1;
    public CafeCoupleNpcController character2;

    public string npcRelationship;
    public string conversationTone;

    public float minConversationWaitingTime = 10f;
    public float maxConversationWaitingTime = 30f;

    Dictionary<string, CafeCoupleNpcController> characters = new Dictionary<string, NpcController>();

    void Start()
    {
        characters[character1.npcName] = character1;
        characters[character2.npcName] = character2;
    }

    public void LoadConversation(List<CafeCoupleNetworkManager.ConversationMessage> conversation)
    {
        Debug.Log("Received conversation: " + JsonConvert.SerializeObject(conversation, Formatting.Indented));
        StartCoroutine(ProcessConversation(conversation));
    }

    IEnumerator ProcessConversation(List<CafeCoupleNetworkManager.ConversationMessage> conversation)
    {
        foreach (var message in conversation)
        {
            if (characters.ContainsKey(message.character))
            {
                CafeCoupleNpcController character = characters[message.character];

                if (characters.ContainsKey(message.target))
                {
                    CafeCoupleNpcController target = characters[message.target];
                    character.Talk(target, message.message);

                    while (character.IsTalking())
                    {
                        yield return null;
                    }
                }
                else
                {
                    Debug.LogError($"Target character {message.target} not found.");
                }
            }
            else
            {
                Debug.LogError($"Character {message.character} not found.");
            }
        }

        Debug.Log("Conversation complete");

        conversationWaitingTime = Random.Range(minConversationWaitingTime, maxConversationWaitingTime);
        yield return new WaitForSeconds(conversationWaitingTime);
        
        SendContinueConversation();
    }

    public void SendBeginConversation()
    {
        cafeCoupleNetworkManager.SendBeginConversation(character1, character2, npcRelationship, conversationTone);
    }

    void SendContinueConversation()
    {
        cafeCoupleNetworkManager.SendContinueConversation(character1, character2, npcRelationship, conversationTone);
    }
}
