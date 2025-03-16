public interface IInteractable
{
    string action { get; set; }
    void Interact();
}

public interface IPerceptible
{
    string type { get; set; }
}

public interface INamed
{
    string name { get; set; }
}