namespace LlmService;

public class SystemMessageDto(string message)
{
    public static readonly SystemMessageDto RequestStarted = new("RequestStarted");
    public static readonly SystemMessageDto ChunkRecieved = new("Chunk recieved");
    public static readonly SystemMessageDto LlmTimeout = new("LLM response timeout");
    public static readonly SystemMessageDto ClientTimeout = new("Client timeout");
    public static readonly SystemMessageDto ModelDoesNotExist = new("Model with this name does not exist");
    public static readonly SystemMessageDto Finished = new("Request finished");
    public string Message { get; init; } = message;
}

