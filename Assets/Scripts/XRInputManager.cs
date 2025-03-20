using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class XRInputManager : MonoBehaviour
{
    public static XRInputManager Instance { get; private set; }

    public InputActionReference AButtonReference;
    public InputActionReference BButtonReference;
    public InputActionReference XButtonReference;
    public InputActionReference YButtonReference;

    public delegate void ButtonStateChangedHandler(bool isPressed);
    public event ButtonStateChangedHandler AButtonPressed;
    public event ButtonStateChangedHandler BButtonPressed;
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
        AButtonReference.action.started += OnAButtonPressed;
        AButtonReference.action.canceled += OnAButtonReleased;

        BButtonReference.action.started += OnBButtonPressed;
        BButtonReference.action.canceled += OnBButtonReleased;

        XButtonReference.action.started += OnXButtonPressed;
        XButtonReference.action.canceled += OnXButtonReleased;

        YButtonReference.action.started += OnYButtonPressed;
        YButtonReference.action.canceled += OnYButtonReleased;
    }

    private void OnDestroy()
    {
        AButtonReference.action.started -= OnAButtonPressed;
        AButtonReference.action.canceled -= OnAButtonReleased;
        AButtonReference.action.Disable();

        BButtonReference.action.started -= OnBButtonPressed;
        BButtonReference.action.canceled -= OnBButtonReleased;
        BButtonReference.action.Disable();

        XButtonReference.action.started -= OnXButtonPressed;
        XButtonReference.action.canceled -= OnXButtonReleased;
        XButtonReference.action.Disable();

        YButtonReference.action.started -= OnYButtonPressed;
        YButtonReference.action.canceled -= OnYButtonReleased;
        YButtonReference.action.Disable();
    }

    void OnAButtonPressed(InputAction.CallbackContext context)
    {
        bool isPressed = context.ReadValueAsButton();
        AButtonPressed?.Invoke(isPressed);
        Debug.Log($"A Button: {isPressed}");
    }

    void OnAButtonReleased(InputAction.CallbackContext context)
    {
        bool isPressed = context.ReadValueAsButton();
        AButtonPressed?.Invoke(isPressed);
        Debug.Log($"A Button: {isPressed}");
    }

    void OnBButtonPressed(InputAction.CallbackContext context)
    {
        bool isPressed = context.ReadValueAsButton();
        BButtonPressed?.Invoke(isPressed);
        Debug.Log($"B Button: {isPressed}");
    }

    void OnBButtonReleased(InputAction.CallbackContext context)
    {
        bool isPressed = context.ReadValueAsButton();
        BButtonPressed?.Invoke(isPressed);
        Debug.Log($"B Button: {isPressed}");
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