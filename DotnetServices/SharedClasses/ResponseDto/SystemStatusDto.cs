using System.Text.Json.Serialization;

namespace SharedClasses.ResponseDto;


[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SystemStatus
{
    InvalidLlmName,
    RequestStarted,
    BehaviourModelInvalid
}

public class SystemStatusDto(SystemStatus status): ResponseDtoBase
{
    public SystemStatus Status { get; set; } = status;
}

