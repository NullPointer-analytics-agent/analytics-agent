namespace SharedClasses.ResponseDto.BehaviourModel;

public class VariableDefinition
{
    public string VariableName { get; set; } = string.Empty;
    public TypeOfVariable VarType { get; set; }
    public int? IntegerMin { get; set; }
    public int? IntegerMax { get; set; }
    public List<string>? EnumValues { get; set; }
}