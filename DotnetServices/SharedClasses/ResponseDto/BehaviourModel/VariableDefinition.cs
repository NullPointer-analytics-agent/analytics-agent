namespace SharedClasses.ResponseDto.BehaviourModel;

public class VariableDefinition
{
    public string VariableName { get; set; } = string.Empty;
    public VariableType VariableType { get; set; }
    public int? IntegerMin { get; set; }
    public int? IntegerMax { get; set; }
    public List<string>? EnumValues { get; set; }
}