namespace SharedClasses.ResponseDto.BehaviourModel;


public class VariableState
{
    public string ReferencedVariableName { get; set; } = string.Empty;
    public VariableType ReferencedVariableType { get; set; }
    public string Value { get; set; } = string.Empty;
}
