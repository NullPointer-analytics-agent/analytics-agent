using System.Text.Json.Serialization;

namespace LlmService;

public class ChunkDto(string chunkText) : SystemMessageDto("Chunk recieved")
{
    [JsonPropertyOrder(1)]
    public string ChunkText { get; init; } = chunkText;

}
