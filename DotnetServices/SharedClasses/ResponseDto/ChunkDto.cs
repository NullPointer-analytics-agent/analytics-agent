namespace SharedClasses.ResponseDto;

public class ChunkDto(string chunkText): ResponseDtoBase
{
    public string ChunkText { get; init; } = chunkText;
}
