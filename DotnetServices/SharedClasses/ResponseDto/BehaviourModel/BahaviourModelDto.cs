using System.Text.Json;


namespace SharedClasses.ResponseDto.BehaviourModel;

public class BehaviourModelDto: ResponseDtoBase
{
    public static new BehaviourModelDto? FromString(string json)
    {
        return JsonSerializer.Deserialize<BehaviourModelDto>(json, OneLine);
    }


    public bool IsValidPrompt { get; set; }
    public string? ReasonIfInvalid { get; set; }

    public List<VariableDefinition>? Definitions { get; set; }
    public List<VariableState>? InitialStates { get; set; }
    public List<Transition>? Transitions { get; set; }

    //public List<Property>? Properties { get; set; }

}