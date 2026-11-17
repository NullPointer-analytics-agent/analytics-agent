namespace SharedClasses.ResponseDto.BehaviourModel;

public class Transition
{
    public string Name { get; set; } = string.Empty;
    public string? Guard { get; set; }
    public List<VariableState> Effects { get; set; } = [];
}
