using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class XRPlayerController : MonoBehaviour
{
    ActionBasedController leftController;
    ActionBasedController rightController;

    void Awake()
    {
        // Get references to the XR controllers
        // rightController = GetComponentsInChildren<ActionBasedController>()[1];
        
        FindControllers();
        SetupControllerCallbacks();
    }

    void FindControllers()
    {
        ActionBasedController[] controllers = GetComponentsInChildren<ActionBasedController>();
        foreach (ActionBasedController controller in controllers)
        {
            if (controller.name.Contains("Left"))
            {
                leftController = controller;
            }
            if (controller.name.Contains("Right"))
            {
                rightController = controller;
            }
        }
    }

    void SetupControllerCallbacks()
    {
        // Right Controller
        rightController.uiPressAction.action.performed += ctx => XRInputManager.Instance.HandleRecordButton(ctx, false);
        rightController.uiPressAction.action.canceled += ctx => XRInputManager.Instance.HandleRecordButton(ctx, false);
    }

    void OnDestroy()
    {
        rightController.uiPressAction.action.performed -= ctx => XRInputManager.Instance.HandleRecordButton(ctx, false);
        rightController.uiPressAction.action.canceled -= ctx => XRInputManager.Instance.HandleRecordButton(ctx, false);
    }
}