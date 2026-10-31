using SharedClasses.ResponseDto;

namespace LlmService;

public static class HttpContextExtensions
{
    public static async Task SendDtoAsync(this HttpContext context, ResponseDtoBase? dto, CancellationToken token)
    {
        if (dto == null) return;
        token.ThrowIfCancellationRequested();
        await context.Response.WriteAsync($"{dto.SerializeOneLine()}\n", token);
    }
}
