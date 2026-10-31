namespace SharedClasses.ResponseDto;


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

