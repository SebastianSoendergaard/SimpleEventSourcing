namespace Basses.SimpleEventStore;

public interface IInstrumentation
{
    void StartingAction(string type, Guid id, object? details = null);
    void FinishedAction(string type, Guid id, object? details = null);
}

public class NullInstrumentation : IInstrumentation
{
    public void StartingAction(string type, Guid id, object? details = null)
    {
    }
    public void FinishedAction(string type, Guid id, object? details = null)
    {
    }
}
