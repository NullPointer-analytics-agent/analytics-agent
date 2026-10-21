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

    private static readonly string prompt =
        "Требования:\n" +
        "Необходимо сформировать переменные состояния.\n" +
        "Доступны 3 типа: Integer, Enum, Boolean.\n" +
        "ВАЖНО: Поле 'EnumValues' должно заполняться ТОЛЬКО если VarType == 'Enum'. " +
        "Для типов Integer и Boolean поле 'EnumValues' должно БЫТЬ СТРОГО null (не пустой список []).\n" +
        "Укажи что ты за llm в поле LlmName\n\n" +
        "Описание явлений:\n" +
        "1. Скорость машины от 0 до 90.\n" +
        "2. Погода на улице (ясно, облачно, дождь).\n" +
        "3. Светло ли на улице.";


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

    private static async Task GenerateAnswerAsync(HttpContext context, Kernel kernel, string modelName, CancellationToken connectionToken)
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

        OpenAIPromptExecutionSettings executionSettings = new()
        {
            ResponseFormat = typeof(BehaviourModel),
            Temperature = 0.2f,
            ServiceId = modelName
        };


        await context.SendDtoAsync(SystemMessageDto.RequestStarted, connectionToken);




        KernelArguments arguments = new (executionSettings);

        IAsyncEnumerable<StreamingKernelContent> result = kernel.InvokePromptStreamingAsync(
            promptTemplate: prompt,
            arguments: arguments,
            cancellationToken: connectionToken
        );

        StringBuilder json = new();


        await foreach (StreamingKernelContent chunk in result)
        {
            string chunkText = chunk.ToString();
            await context.SendDtoAsync(new ChunkDto(chunkText), connectionToken);
            json.Append(chunkText);
        }
        

        
        BehaviourModel? behaviourModel = JsonSerializer.Deserialize<BehaviourModel>(json.ToString(), serializerOptions); 
        
        await context.SendDtoAsync(behaviourModel, connectionToken);

        await context.SendDtoAsync(SystemMessageDto.Finished, connectionToken);
    }
}

