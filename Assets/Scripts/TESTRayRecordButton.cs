using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class TESTRayRecordButton : MonoBehaviour
{
    RaycastHit raycastHit;
    LayerMask layerMask;
    RaycastHit NpcImTalkingTo;


    void Start()
    {
        layerMask = LayerMask.GetMask("NPC");
        XRInputManager.Instance.AButtonPressed += HandleRecordButton;
    }

    void OnDestroy()
    {
        XRInputManager.Instance.AButtonPressed -= HandleRecordButton;
    }

    void FixedUpdate()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, 10, layerMask))
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
            Debug.Log("Did Hit");
            raycastHit = hit;
        }
        else
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 10, Color.white);
            raycastHit = hit;
        }
    }

    void HandleRecordButton(bool isPressed)
    {
        if (isPressed)
        {
            Debug.Log("hit: " + raycastHit);
            if (raycastHit.collider != null)
            {
                NpcImTalkingTo = raycastHit;
                TellNpcWhatISaid("yo my guy");
            }
        }
        else
        {
            Debug.Log("released");
        }
    }

    // tell that person what you said
    void TellNpcWhatISaid(string words)
    {
        NpcControllerOld npc = NpcImTalkingTo.collider.GetComponent<NpcControllerOld>();
        npc.Tell(words);
    }
}