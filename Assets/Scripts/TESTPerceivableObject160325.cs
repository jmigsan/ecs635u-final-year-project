using System;
using UnityEngine;

public class TESTPerceivableObject160325 : MonoBehaviour, IPerceptible
{
    [Header("Object Information")]
    [SerializeField] string _entityName = "Object_" + Guid.NewGuid().ToString();
    [SerializeField] string _type = "object";
    [SerializeField] string _description = "A generic object";

    public string entityName { 
        get { return _entityName; } 
        set { _entityName = value; } 
        }
    public string type { 
        get { return _type; } 
        set { _type = value; } 
        }
    public string description { 
        get { return _description; } 
        set { _description = value; } 
        }

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public Transform GetTransform()
    {
        return transform;
    }
}
