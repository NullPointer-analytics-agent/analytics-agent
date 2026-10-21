namespace LlmService;

public class SystemMessageDto(string message)
{
    public static readonly SystemMessageDto RequestStarted = new("Request Started");
    public static readonly SystemMessageDto ChunkRecieved = new("Chunk recieved");
    public static readonly SystemMessageDto Timeout = new("Response timeout");
    public static readonly SystemMessageDto ModelDoesNotExist = new("Model with this name does not exist");
    public static readonly SystemMessageDto Finished = new("Request finished");
    public string Message { get; init; } = message;
}

