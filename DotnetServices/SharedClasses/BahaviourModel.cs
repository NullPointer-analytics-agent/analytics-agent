namespace SharedClasses;

public class BehaviourModel
{
    public string LlmName { get; set; } = string.Empty;
    public List<StateVariable> Variables { get; set; } = [];
}