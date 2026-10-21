using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
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

    private static KernelArguments GetKernelArguments(string modelName)
    {
        OpenAIPromptExecutionSettings executionSettings = new()
        {
            ResponseFormat = typeof(BehaviourModel),
            Temperature = 0.2f,
            ServiceId = modelName
        };
        return new(executionSettings);
    }

    private static readonly string prompt =
        "Требования:\n" +
        "Необходимо сформировать переменные состояния.\n" +
        "Доступны 3 типа: Integer, Enum, Boolean.\n" +
        "ВАЖНО: Поле 'EnumValues' должно заполняться ТОЛЬКО если VarType == 'Enum'. " +
        "Для типов Integer и Boolean поле 'EnumValues' должно БЫТЬ СТРОГО null (не пустой список []).\n" +
        "также необходимо заполнить текущие значения переменных\n" +
        "Укажи что ты за llm в поле LlmName\n\n" +
        "Описание явлений:\n" +
        "1. Скорость машины от 0 до 90.\n" +
        "2. Погода на улице (ясно, облачно, дождь).\n" +
        "3. Светло ли на улице." +
        "4. Начальное состояние: Машина едет со скоростью 45 км/ч в дождь ночью. ";


    private static async Task SendDtoAsync(this HttpContext context, object? dto, CancellationToken token)
    {
        if (dto == null) return;
        token.ThrowIfCancellationRequested();
        await context.Response.WriteAsync($"{JsonSerializer.Serialize(dto, serializerOptions)}\n", token);
    }

    public static IEndpointRouteBuilder MapLLMChatEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("api/chat");

        group.MapGet("/test", GenerateAnswerAsync);

        return app;
    }

    private static async Task GenerateAnswerAsync(
        HttpContext context,
        Kernel kernel,
        CancellationToken connectionToken,
        [FromQuery]string? modelName = "qwen")
    {
        context.Response.ContentType = "text/x-ndjson; charset=utf-8";


        try
        {
            kernel.GetRequiredService<IChatCompletionService>(modelName);
        }
        catch (KernelException)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.SendDtoAsync(SystemMessageDto.ModelDoesNotExist, connectionToken);
            return;
        }


        
        await context.SendDtoAsync(SystemMessageDto.RequestStarted, connectionToken);


        StringBuilder json = new();


        CancellationTokenSource timeoutTokenSource = new();


        timeoutTokenSource.CancelAfter(TimeSpan.FromSeconds(30)); // ожидание пока LLM проснется

        CancellationTokenSource linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            connectionToken,
            timeoutTokenSource.Token
        );

        IAsyncEnumerable<StreamingKernelContent> result = kernel.InvokePromptStreamingAsync(
            promptTemplate: prompt,
            arguments: GetKernelArguments(modelName),
            cancellationToken: linkedTokenSource.Token
        );


        try
        {
            await foreach (StreamingKernelContent chunk in result.WithCancellation(linkedTokenSource.Token))
            {
                string chunkText = chunk.ToString();
                timeoutTokenSource.CancelAfter(TimeSpan.FromSeconds(3)); // время на отправку чанка

                await context.SendDtoAsync(new ChunkDto(chunkText), linkedTokenSource.Token); // сколько в худшем случае будет длиться это?
                json.Append(chunkText);
 
                timeoutTokenSource.CancelAfter(TimeSpan.FromSeconds(10)); // ожидание следующего токена после первого успешного получения
            }
        }

        catch (OperationCanceledException)
        {
            if (!connectionToken.IsCancellationRequested)
            {
                await context.SendDtoAsync(SystemMessageDto.Timeout, connectionToken);
            }
            return;
        }
       
        
        BehaviourModel? behaviourModel = JsonSerializer.Deserialize<BehaviourModel>(json.ToString(), serializerOptions); 
        
        await context.SendDtoAsync(behaviourModel, connectionToken);

        await context.SendDtoAsync(SystemMessageDto.Finished, connectionToken);
    }
}

