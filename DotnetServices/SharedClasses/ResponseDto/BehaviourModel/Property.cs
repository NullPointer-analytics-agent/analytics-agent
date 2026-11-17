namespace SharedClasses.ResponseDto.BehaviourModel;


public enum PropertyType { Invariant, Reachable, ForbiddenState}

public class Property
{
    public PropertyType PropertyType { get; set; }
    public Expression? Expression { get; set; }
}
