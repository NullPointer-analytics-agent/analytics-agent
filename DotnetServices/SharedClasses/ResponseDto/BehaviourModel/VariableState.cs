namespace SharedClasses.ResponseDto.BehaviourModel;


public class VariableState
{
    public string VariableName { get; set; } = string.Empty;
    public int? IntegerValue { get; set; }
    public string? EnumValue { get; set; }
    public bool? BooleanValue { get; set; }
}

