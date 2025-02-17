using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class XRInputManager : MonoBehaviour
{
    public static XRInputManager Instance { get; private set; }

    public InputActionReference RecordButtonReference; // Drag your Input Action Reference here in the Inspector
    
    public delegate void ButtonStateChangedHandler(bool isPressed);
    public event ButtonStateChangedHandler RecordButtonPressed;

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
    void Start()
    {
        RecordButtonReference.action.started += OnRecordButtonPressed;
        RecordButtonReference.action.canceled += OnRecordButtonReleased;
    }

    private void OnDestroy()
    {
        RecordButtonReference.action.started -= OnRecordButtonPressed; // Unsubscribe to prevent memory leaks
        RecordButtonReference.action.canceled -= OnRecordButtonReleased;
        RecordButtonReference.action.Disable(); // Disable the Input Action when the script is destroyed
    }

    void OnRecordButtonPressed(InputAction.CallbackContext context)
    {
        bool isPressed = context.ReadValueAsButton();
        RecordButtonPressed?.Invoke(isPressed);
        Debug.Log($"Record Button: {isPressed}");
    }

    void OnRecordButtonReleased(InputAction.CallbackContext context)
    {
        bool isPressed = context.ReadValueAsButton();
        RecordButtonPressed?.Invoke(isPressed);
        Debug.Log($"Record Button: {isPressed}");
    }
}