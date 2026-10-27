namespace SharedClasses.ResponseDto.BehaviourModel;

public class VariableValue
{
    public string VariableName { get; set; } = string.Empty;

    public int? IntegerCurrentValue { get; set; }

    public string? EnumCurrentValue { get; set; }

    public bool? BooleanCurrentValue { get; set; }
}
