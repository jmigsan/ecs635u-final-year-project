using UnityEngine;

public interface INamed
{
    string entityName { get; set; }
}

public interface IPerceptible : INamed
{
    string type { get; set; }
    Vector3 GetPosition();
}

public interface IActionable : IPerceptible
{
    string action { get; set; }
    void Action();
}