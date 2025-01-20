namespace EventSourcing.Projections;

public class ProjectorException : Exception
{
    public ProjectorException(string message) : base(message) { }
    public ProjectorException(string message, Exception exception) : base(message, exception) { }
}

public class NotFoundException : ProjectorException
{
    public NotFoundException(string message) : base(message) { }
}
