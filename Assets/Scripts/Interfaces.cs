using System.Collections.Generic;
using UnityEngine;

public interface INamed
{
    string entityName { get; set; }
}

public interface IPerceptible : INamed
{
    string type { get; set; }
    string description { get; set; }
    Vector3 GetPosition();
    Transform GetTransform();
}

public interface IActionable : IPerceptible 
{
    List<string> nearActions { get; set; }
    List<string> farActions { get; set; }
}
