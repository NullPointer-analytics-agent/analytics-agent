namespace SharedClasses.ResponseDto.BehaviourModel;


public enum PropertyType { Invariant, Reachable, ForbiddenState, DeadlockFree}

public class Property
{
    public PropertyType PropertyType { get; set; }
    public string Guard { get; set; } = string.Empty;
}
