using System.Text.Json;


namespace SharedClasses.ResponseDto.BehaviourModel;

public class BehaviourModelDto: ResponseDtoBase
{
    public string? Reasoning { get; set; }

    public static new BehaviourModelDto? FromString(string json)
    {
        return JsonSerializer.Deserialize<BehaviourModelDto>(json, OneLine);
    }
    public string LlmName { get; set; } = string.Empty;
    public bool IsValidPrompt { get; set; }
    public string? ReasonIfInvalid { get; set; }

    public List<VariableDefinition>? Definitions { get; set; }
    public List<VariableState>? InitialStates { get; set; }
    public ExpressionElement? Expression { get; set; }
}