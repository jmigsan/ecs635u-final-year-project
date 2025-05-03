using UnityEngine;
using UnityEngine.InputSystem; // Required for new Input System

public class TESTBeginStoryTrigger160325 : MonoBehaviour
{
    [Header("Game Managers")]
    public CafeCoupleGameManager cafeCoupleGameManager1;
    public CafeCoupleGameManager cafeCoupleGameManager2;
    public IndividualReservedGameManager individualReservedGameManager;
    
    [Header("Manager Activation Settings")]
    public bool activateNetworkManager = false;  // Inspector toggle for network manager
    public bool activateManager1 = false;  // Inspector toggle for manager 1
    public bool activateManager2 = false;  // Inspector toggle for manager 2
    public bool activateManager3 = false;  // Inspector toggle for manager 2
    
    private bool hasTriggered = false; // Flag to prevent multiple triggers
    private Keyboard keyboard;

    private void Awake()
    {
        // Get keyboard device
        keyboard = Keyboard.current;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the colliding object is the player
        if (!hasTriggered && other.CompareTag("Player"))
        {
            TriggerStory();
        }
    }

    private void Update()
    {
        // Check if keyboard exists (important for non-keyboard platforms)
        if (keyboard != null)
        {
            // Check specifically for the 'S' key being pressed down this frame
            if (!hasTriggered && keyboard.sKey.wasPressedThisFrame)
            {
                TriggerStory();
            }
        }
    }

    private void TriggerStory()
    {
        Debug.Log("Begin Story trigger activated by S key or collision");
        
        // Only activate NetworkManager based on inspector setting
        if (activateNetworkManager && NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendBeginStory();
            Debug.Log("Activated NetworkManager");
        }
        
        // Only activate managers based on inspector settings
        if (activateManager1 && cafeCoupleGameManager1 != null)
        {
            cafeCoupleGameManager1.SendBeginConversation();
            Debug.Log("Activated CafeCoupleGameManager1");
        }
        
        if (activateManager2 && cafeCoupleGameManager2 != null)
        {
            cafeCoupleGameManager2.SendBeginConversation();
            Debug.Log("Activated CafeCoupleGameManager2");
        }

        if (activateManager3 && individualReservedGameManager != null)
        {
            individualReservedGameManager.SendInitialiseCharacterMessage();
            Debug.Log("Activated IndividualReservedGameManager");
        }
        
        hasTriggered = true;

        // Optionally, you can destroy the trigger after it's been used
        Destroy(gameObject);
    }
}
