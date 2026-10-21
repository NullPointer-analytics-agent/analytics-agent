namespace SharedClasses;

public class BehaviourModel
{
    public string LlmName { get; set; } = string.Empty;
    public List<VariableDefinition> Definitions { get; set; } = [];
    public List<VariableValue> Values { get; set; } = [];
}