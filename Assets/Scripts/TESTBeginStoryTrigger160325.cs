using UnityEngine;
using UnityEngine.InputSystem; // Required for new Input System

public class TESTBeginStoryTrigger160325 : MonoBehaviour
{
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
        TESTNetworkManager140325.Instance.SendBeginStory();
        hasTriggered = true;
        
        // Optionally, you can destroy the trigger after it's been used
        Destroy(gameObject);
    }
}
