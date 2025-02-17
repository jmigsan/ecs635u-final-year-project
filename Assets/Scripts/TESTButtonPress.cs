using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleButtonPress : MonoBehaviour
{
    public InputActionReference buttonActionReference; // Drag your Input Action Reference here in the Inspector
    public GameObject objectToActivate; // Optional: Drag an object to activate here

    private InputAction buttonAction;

    private void Start()
    {
        buttonAction = buttonActionReference.action;

        if (buttonAction != null)
        {
            buttonAction.started += OnButtonPressed; // Subscribe to the 'performed' event
            buttonAction.canceled += OnButtonReleased; // Subscribe to the 'performed' event
            buttonAction.Enable(); // Enable the Input Action
        }
        else
        {
            Debug.LogError("Button Action is not assigned! Please assign an InputActionReference in the Inspector.");
        }

        if (objectToActivate != null)
        {
            objectToActivate.SetActive(false); // Initially deactivate the object (optional)
        }
    }

    private void OnDestroy()
    {
        if (buttonAction != null)
        {
            buttonAction.started -= OnButtonPressed; // Unsubscribe to prevent memory leaks
            buttonAction.canceled -= OnButtonReleased; // Unsubscribe to prevent memory leaks
            buttonAction.Disable(); // Disable the Input Action when the script is destroyed
        }
    }

    private void OnButtonPressed(InputAction.CallbackContext context)
    {
        Debug.Log("Button Pressed!"); // Example: Log a message to the console

        if (objectToActivate != null)
        {
            objectToActivate.SetActive(!objectToActivate.activeSelf); // Example: Toggle object activation
        }

        // Add your code here to do something when the button is pressed!
    }

    private void OnButtonReleased(InputAction.CallbackContext context)
    {
        Debug.Log("Button Released!"); // Example: Log a message to the console

        // Add your code here to do something when the button is pressed!
    }
}