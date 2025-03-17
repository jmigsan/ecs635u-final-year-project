using System;
using UnityEngine;
using System.Collections.Generic;

public class TESTActionableObject160325 : MonoBehaviour, IActionable, IPerceptible
{
    [Header("Object Information")]
    [SerializeField] string _entityName = "Actionable_" + Guid.NewGuid().ToString();
    [SerializeField] string _type = "actionable";
    [SerializeField] string _description = "A generic actionable object";
    [SerializeField] List<string> _nearActions = new List<string>();
    [SerializeField] List<string> _farActions = new List<string>();

    public string entityName
    {
        get { return _entityName; }
        set { _entityName = value; }
    }
    public string type
    {
        get { return _type; }
        set { _type = value; }
    }
    public string description
    {
        get { return _description; }
        set { _description = value; }
    }
    public List<string> nearActions { get => _nearActions; set => _nearActions = value; }
    public List<string> farActions { get => _farActions; set => _farActions = value; }

    public Vector3 GetPosition() => transform.position;

    public Transform GetTransform() => transform;
}
