using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SharedClasses;
using SharedClasses.ResponseDto;
namespace LlmService;

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapLLMChatEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("api/chat");

        group.MapPost("/", GenerateAnswerAsync);

        return app;
    }


    private static async Task GenerateAnswerAsync(
        HttpContext context,
        Kernel kernel,
        IPromptHandler handler, 
        PromptParameters promptParameters,
        UserRequestDto userRequest,
        CancellationToken connectionToken,
        string? modelName = "qwen"

    )
    {
        string userPrompt = userRequest.Prompt;

        context.Response.ContentType = "text/x-ndjson; charset=utf-8";

        IChatCompletionService? completionService = null;

        try
        {
            completionService = kernel.GetRequiredService<IChatCompletionService>(modelName);
        }
        catch (KernelException)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.SendDtoAsync(new SystemStatusDto(SystemStatus.LlmNameNotFound), connectionToken);
            return;
        }

        IAsyncEnumerable<ResponseDtoBase> response = handler.GenerateAnswerAsync(
            userPrompt,
            promptParameters,
            completionService,
            connectionToken
        );

        await foreach(ResponseDtoBase part in response)
        {
            await context.SendDtoAsync(part, connectionToken);
        }
    }
}
