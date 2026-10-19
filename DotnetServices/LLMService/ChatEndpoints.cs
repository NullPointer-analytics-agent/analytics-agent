using Microsoft.SemanticKernel;

namespace LLMChatService;

public static class ChatEndpoints
{

    public static IEndpointRouteBuilder MapLLMChatEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("api/chat");

        group.MapGet("/test", Get);

        return app;
    }

    private static IResult Get(Kernel kernel, CancellationToken connectionToken)
    {
        return Results.Stream(
            outputStream => StreamText(outputStream, kernel, connectionToken),
            contentType: "text/event-stream; charset=utf-8"
        );
    }

    private static async Task StreamText(Stream outputStream, Kernel kernel, CancellationToken connectionToken)
    {
        string prompt = "Привет, напиши hello world на C# с комментариями";


        await using StreamWriter writer = new(outputStream);

        writer.Write("Starting request...\n");
        await writer.FlushAsync(connectionToken);


        using CancellationTokenSource timeoutSource = new();

        timeoutSource.CancelAfter(TimeSpan.FromSeconds(20));

            
        using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            connectionToken,
            timeoutSource.Token
        );


        IAsyncEnumerable<StreamingKernelContent> result = kernel.InvokePromptStreamingAsync(
            prompt,
            cancellationToken: linkedSource.Token
        );

        try
        {
            await foreach (StreamingKernelContent chunk in result)
            {
                try
                {
                    writer.Write(chunk.ToString());
                    await writer.FlushAsync(linkedSource.Token);
                }
                catch (OperationCanceledException) { break; }
                timeoutSource.CancelAfter(TimeSpan.FromSeconds(10));
            }
        }
        catch (OperationCanceledException) {}
    }
}

