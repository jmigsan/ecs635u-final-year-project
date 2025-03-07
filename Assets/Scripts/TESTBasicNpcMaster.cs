using UnityEngine;
using UnityEngine.InputSystem;

public class TESTBasicNpcMaster : MonoBehaviour
{
    public GameObject npc;
    private TESTBasicNpcControl targetNPC;
    public Transform chair;
    public Transform table;
    
    // Input Action reference - you'll need to set this up in the Input Actions asset
    private InputAction testAction;
    private InputAction moveToChairAction;
    private InputAction sitAction;
    private InputAction talkAction;
    private InputAction idleAction;
    private InputAction moveToTableAction;
    private InputAction stopAction;
    
    private void Awake()
    {
        // Create input actions
        testAction = new InputAction("Test", binding: "<Keyboard>/0");
        moveToChairAction = new InputAction("MoveToChair", binding: "<Keyboard>/1");
        sitAction = new InputAction("Sit", binding: "<Keyboard>/2");
        talkAction = new InputAction("Talk", binding: "<Keyboard>/3");
        idleAction = new InputAction("Idle", binding: "<Keyboard>/4");
        moveToTableAction = new InputAction("MoveToTable", binding: "<Keyboard>/5");
        stopAction = new InputAction("Stop", binding: "<Keyboard>/6");
        
        // Register callbacks
        testAction.performed += ctx => OnTest();
        moveToChairAction.performed += ctx => OnMoveToChair();
        sitAction.performed += ctx => OnSit();
        talkAction.performed += ctx => OnTalk();
        idleAction.performed += ctx => OnIdle();
        moveToTableAction.performed += ctx => OnMoveToTable();
        stopAction.performed += ctx => OnStop();
    }
    
    private void OnEnable()
    {
        // Enable all actions
        testAction.Enable();
        moveToChairAction.Enable();
        sitAction.Enable();
        talkAction.Enable();
        idleAction.Enable();
        moveToTableAction.Enable();
        stopAction.Enable();
    }
    
    private void OnDisable()
    {
        // Disable all actions
        testAction.Disable();
        moveToChairAction.Disable();
        sitAction.Disable();
        talkAction.Disable();
        idleAction.Disable();
        moveToTableAction.Disable();
        stopAction.Disable();
    }

    void Start()
    {
        // Check if npc reference is assigned
        if (npc == null)
        {
            Debug.LogError("NPC reference is missing! Please assign it in the inspector.");
            return;
        }

        // Get and store the NPC control component
        targetNPC = npc.GetComponent<TESTBasicNpcControl>();
        
        if (targetNPC == null)
        {
            Debug.LogError("TESTBasicNpcControl component not found on the assigned NPC!");
        }

        Debug.Log("NPC Master initialized successfully");
    }
    
    // Input action callbacks
    private void OnTest()
    {
        Debug.Log("Test key (0) pressed - Input detection is working");
    }
    
    private void OnMoveToChair()
    {
        Debug.Log("Command: Move to chair");
        if (targetNPC != null && chair != null)
        {
            targetNPC.Move(chair.position);
        }
        else if (chair == null)
        {
            Debug.LogError("Chair reference is missing!");
        }
    }
    
    private void OnSit()
    {
        Debug.Log("Command: Sit");
        if (targetNPC != null)
        {
            targetNPC.Sit();
        }
    }
    
    private void OnTalk()
    {
        Debug.Log("Command: Talk");
        if (targetNPC != null)
        {
            targetNPC.Talk("Hello there!");
        }
    }
    
    private void OnIdle()
    {
        Debug.Log("Command: Idle");
        if (targetNPC != null)
        {
            targetNPC.Idle();
        }
    }
    
    private void OnMoveToTable()
    {
        Debug.Log("Command: Move to table");
        if (targetNPC != null && table != null)
        {
            targetNPC.Move(table.position);
        }
        else if (table == null)
        {
            Debug.LogError("Table reference is missing!");
        }
    }
    
    private void OnStop()
    {
        Debug.Log("Command: Stop");
        if (targetNPC != null)
        {
            targetNPC.Stop();
        }
    }
}