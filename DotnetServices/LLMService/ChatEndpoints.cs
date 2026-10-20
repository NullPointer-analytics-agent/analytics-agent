using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Chat;
using SharedClasses;

namespace LlmService;

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
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ResponseFormat = typeof(BehaviourModel),
            Temperature = 0.2f
        };



        string prompt = "Требования:\n" +
                "Необходимо сформировать переменные состояния.\n" +
                "Доступны 3 типа: Integer, Enum, Boolean.\n" +
                "ВАЖНО: Поле 'EnumValues' должно заполняться ТОЛЬКО если VarType == 'Enum'. " +
                "Для типов Integer и Boolean поле 'EnumValues' должно БЫТЬ СТРОГО null (не пустой список []).\n\n" +
                "Описание явлений:\n" +
                "1. Скорость машины от 0 до 90.\n" +
                "2. Погода на улице (ясно, облачно, дождь).\n" +
                "3. Светло ли на улице.";



        await using StreamWriter writer = new(outputStream);

        writer.Write("Starting request...\n");
        await writer.FlushAsync(connectionToken);


        using CancellationTokenSource timeoutSource = new();

        timeoutSource.CancelAfter(TimeSpan.FromSeconds(40));

            
        using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            connectionToken,
            timeoutSource.Token
        );

        KernelArguments arguments = new (executionSettings);

        IAsyncEnumerable<StreamingKernelContent> result = kernel.InvokePromptStreamingAsync(
            promptTemplate: prompt,
            arguments: arguments,
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

