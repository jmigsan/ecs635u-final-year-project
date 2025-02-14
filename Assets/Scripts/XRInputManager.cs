using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class XRInputManager : MonoBehaviour
{
    public static XRInputManager Instance { get; private set; }
    
    public delegate void OnButtonStateChangedHandler(bool isPressed, bool isLeftHand);
    public event OnButtonStateChangedHandler OnRecordButtonPressed;

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

    public void HandleRecordButton(InputAction.CallbackContext context, bool isLeftHand)
    {
        bool isPressed = context.ReadValueAsButton();
        OnRecordButtonPressed?.Invoke(isPressed, isLeftHand);
        Debug.Log($"{(isLeftHand ? "Left" : "Right")} Record Button: {isPressed}");
    }
}