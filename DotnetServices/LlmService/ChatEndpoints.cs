using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Chat;
using SharedClasses;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace LlmService;

public static class ChatEndpoints
{
    private static readonly JsonSerializerOptions serializerOptions = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.Cyrillic, UnicodeRanges.BasicLatin)
    };

    private static readonly string prompt = "Требования:\n" +
                "Необходимо сформировать переменные состояния.\n" +
                "Доступны 3 типа: Integer, Enum, Boolean.\n" +
                "ВАЖНО: Поле 'EnumValues' должно заполняться ТОЛЬКО если VarType == 'Enum'. " +
                "Для типов Integer и Boolean поле 'EnumValues' должно БЫТЬ СТРОГО null (не пустой список []).\n" +
                "Укажи что ты за llm в поле LlmName\n\n" +
                "Описание явлений:\n" +
                "1. Скорость машины от 0 до 90.\n" +
                "2. Погода на улице (ясно, облачно, дождь).\n" +
                "3. Светло ли на улице.";

    private static async Task SendDtoAsync(this StreamWriter writer, object? dto, CancellationToken token)
    {
        if (dto == null) return;
        writer.WriteLine(JsonSerializer.Serialize(dto, serializerOptions));
        await writer.FlushAsync(token);
    }

    public static IEndpointRouteBuilder MapLLMChatEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("api/chat");

        group.MapGet("/test", Get);

        return app;
    }

    private static IResult Get(
        Kernel kernel,
        CancellationToken connectionToken,
        [FromQuery] string modelName = "qwen")
    {
        return Results.Stream(
            outputStream => GenerateAnswerAsync(outputStream, kernel, modelName, connectionToken),
            contentType: "text/x-ndjson; charset=utf-8"
        );
    }

    private static async Task GenerateAnswerAsync(Stream outputStream, Kernel kernel, string modelName, CancellationToken connectionToken)
    {
        await using StreamWriter writer = new(outputStream);


        try
        {
            kernel.GetRequiredService<IChatCompletionService>(modelName);
        }
        catch (Exception)
        {
            await writer.SendDtoAsync(SystemMessageDto.ModelDoesNotExist, connectionToken);
            return;
        }

        OpenAIPromptExecutionSettings executionSettings = new()
        {
            ResponseFormat = typeof(BehaviourModel),
            Temperature = 0.2f,
            ServiceId = modelName
        };


        await writer.SendDtoAsync(SystemMessageDto.RequestStarted, connectionToken);


        using CancellationTokenSource timeoutSource = new();

        timeoutSource.CancelAfter(TimeSpan.FromSeconds(20));

            
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
        StringBuilder json = new();

        try
        {
            await foreach (StreamingKernelContent chunk in result)
            {

                try
                {
                    string chunkText = chunk.ToString();
                    await writer.SendDtoAsync(new ChunkDto(chunkText), linkedSource.Token);
                    json.Append(chunkText);
                }
                catch (OperationCanceledException)
                {
                    await writer.SendDtoAsync(SystemMessageDto.ClientTimeout, linkedSource.Token);
                    return;
                }
                timeoutSource.CancelAfter(TimeSpan.FromSeconds(10));
            }
        }
        catch (OperationCanceledException)
        {
            await writer.SendDtoAsync(SystemMessageDto.LlmTimeout, linkedSource.Token);
            return;
        }

        
        BehaviourModel? behaviourModel = JsonSerializer.Deserialize<BehaviourModel>(json.ToString(), serializerOptions);
        
        //TODO: Dto validation
        await writer.SendDtoAsync(behaviourModel, linkedSource.Token);

        await writer.SendDtoAsync(SystemMessageDto.Finished, linkedSource.Token);
    }
}

