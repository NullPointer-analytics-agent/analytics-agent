namespace SharedClasses.ResponseDto.BehaviourModel;


public class VariableState
{
    public string ReferencedVariableName { get; set; } = string.Empty;
    public VariableType ReferencedVariableType { get; set; }
    public int? IntegerValue { get; set; }
    public string? EnumValue { get; set; }
    public bool? BooleanValue { get; set; }
}
