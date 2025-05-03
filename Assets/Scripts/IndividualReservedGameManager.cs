using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections;

public class IndividualReservedGameManager : MonoBehaviour
{
    public IndividualReservedNetworkManager individualReservedNetworkManager;

    public IndividualReservedNpcController character;
    public IPerceptible player;

    public void MakeNpcTalk(string target, string message)
    {
        if (target == "Player")
        {
            character.Talk(player, message);           
        }
        else
        {
            Debug.Log("This shouldn't happen. Something broke.");
        }
    }

    public void SendInitialiseCharacterMessage()
    {
        individualReservedNetworkManager.SendInitialiseCharacterMessage(character);
    }

    public void SendPlayerInterruptionMessage(string playerMessage)
    {
        individualReservedNetworkManager.SendPlayerInterruptionMessage(playerMessage);
    }
}