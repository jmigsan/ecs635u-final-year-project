using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class XRInputManager : MonoBehaviour
{
    public static XRInputManager Instance { get; private set; }

    public InputActionReference RecordButtonReference; // Drag your Input Action Reference here in the Inspector
    public InputActionReference XButtonReference; // Drag your Input Action Reference here in the Inspector
    public InputActionReference YButtonReference; // Drag your Input Action Reference here in the Inspector
    
    public delegate void ButtonStateChangedHandler(bool isPressed);
    public event ButtonStateChangedHandler RecordButtonPressed;
    public event ButtonStateChangedHandler XButtonPressed;
    public event ButtonStateChangedHandler YButtonPressed;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    void Start()
    {
        RecordButtonReference.action.started += OnRecordButtonPressed;
        RecordButtonReference.action.canceled += OnRecordButtonReleased;

        XButtonReference.action.started += OnXButtonPressed;
        XButtonReference.action.canceled += OnXButtonReleased;
        YButtonReference.action.started += OnYButtonPressed;
        YButtonReference.action.canceled += OnYButtonReleased;
    }

    private void OnDestroy()
    {
        RecordButtonReference.action.started -= OnRecordButtonPressed; // Unsubscribe to prevent memory leaks
        RecordButtonReference.action.canceled -= OnRecordButtonReleased;
        RecordButtonReference.action.Disable(); // Disable the Input Action when the script is destroyed

        XButtonReference.action.started -= OnXButtonPressed;
        XButtonReference.action.canceled -= OnXButtonReleased;
        XButtonReference.action.Disable();

        YButtonReference.action.started -= OnYButtonPressed;
        YButtonReference.action.canceled -= OnYButtonReleased;
        YButtonReference.action.Disable();
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

    void OnXButtonPressed(InputAction.CallbackContext context)
    {
        bool isPressed = context.ReadValueAsButton();
        XButtonPressed?.Invoke(isPressed);
        Debug.Log($"X Button: {isPressed}");
    }

    void OnXButtonReleased(InputAction.CallbackContext context)
    {
        bool isPressed = context.ReadValueAsButton();
        XButtonPressed?.Invoke(isPressed);
        Debug.Log($"X Button: {isPressed}");
    }

    void OnYButtonPressed(InputAction.CallbackContext context)
    {
        bool isPressed = context.ReadValueAsButton();
        YButtonPressed?.Invoke(isPressed);
        Debug.Log($"Y Button: {isPressed}");
    }

    void OnYButtonReleased(InputAction.CallbackContext context)
    {
        bool isPressed = context.ReadValueAsButton();
        YButtonPressed?.Invoke(isPressed);
        Debug.Log($"Y Button: {isPressed}");
    }
}