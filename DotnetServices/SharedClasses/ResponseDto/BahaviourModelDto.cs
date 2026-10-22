using System.Text.Json;

namespace SharedClasses.ResponseDto;

public class BehaviourModelDto: ResponseDtoBase
{
    public static new BehaviourModelDto? FromString(string json)
    {
        return JsonSerializer.Deserialize<BehaviourModelDto>(json, OneLine);
    }
    public string LlmName { get; set; } = string.Empty;
    public List<VariableDefinition> Definitions { get; set; } = [];
    public List<VariableValue> Values { get; set; } = [];
}