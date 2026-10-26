using System.Text.Json.Serialization;

namespace SharedClasses.ResponseDto;


[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SystemStatus
{
    LlmNameNotFound,
    RequestStarted,
    LlmTimeout,
    IterationTimeout,
    BehaviourModelSerializationError,
    Finished
}

public class SystemStatusDto(SystemStatus status): ResponseDtoBase
{
    public SystemStatus Status { get; set; } = status;
}

