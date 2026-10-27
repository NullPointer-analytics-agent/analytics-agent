using System.Text.Json;

namespace SharedClasses.ResponseDto.BehaviourModel;

public class BehaviourModelDto: ResponseDtoBase
{
    public static new BehaviourModelDto? FromString(string json)
    {
        return JsonSerializer.Deserialize<BehaviourModelDto>(json, OneLine);
    }
    
    public List<VariableDefinition> Definitions { get; set; } = [];
    public List<VariableValue> Values { get; set; } = [];
    public ExpressionElement? RootExpression { get; set; }
    public string LlmName { get; set; } = string.Empty;
    public bool IsValidPrompt { get; set; }
    public string? ReasonIfInvalid { get; set; }
}