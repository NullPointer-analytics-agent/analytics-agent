using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SharedClasses;
using SharedClasses.ResponseDto;
using System.Text;

namespace LlmService;

public static class ChatEndpoints
{
    private const string SystemPrompt = """
    System: Твоя задача — проанализировать запрос пользователя, определить его валидность и извлечь переменные состояния.

    ОСНОВНЫЕ ПРАВИЛА:
    1. Имя модели (LlmName): Укажи свое реальное коммерческое название (например: "Qwen", "GPT-4o", "DeepSeek-R1"). Не используй заглушки.
    2. Язык идентификаторов (VariableName): Имена всех переменных пиши строго на английском языке в стиле CamelCase или snake_case (например: 'carSpeed', 'weatherCondition', 'isDaylight').
    3. Значения Enum (EnumValues): Пиши элементы перечислений на английском языке. Поле заполняется ТОЛЬКО если VarType == 'Enum', иначе должно быть null.

    КРИТЕРИИ ВАЛИДНОСТИ (IsValidPrompt):
    - Выстави true, если пользователь четко описал список явлений/состояний И указал их начальную конфигурацию (исходные значения).
    - Выстави false, если запрос не по теме (просьбы посчитать, написать код, рассказать фильм) или в тексте не хватает данных для инициализации значений. В этом случае обязательно заполни поле 'ReasonIfInvalid' конкретной причиной на русском языке. Для валидных запросов оставь его пустым.
    
    """;

    private static OpenAIPromptExecutionSettings GetExecutionSettings(string modelName)
    {
        return new()
        {
            ResponseFormat = typeof(BehaviourModelDto),
            Temperature = 0.2f,
            ServiceId = modelName
        };
    }


    private static async Task SendDtoAsync(this HttpContext context, ResponseDtoBase? dto, CancellationToken token)
    {
        if (dto == null) return;
        token.ThrowIfCancellationRequested();
        await context.Response.WriteAsync($"{dto.SerializeOneLine()}\n", token);
    }

    public static IEndpointRouteBuilder MapLLMChatEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("api/chat");

        group.MapPost("/test", ExecutePromptAsync);

        return app;
    }

    private static Task ExecutePromptAsync(
        HttpContext context,
        Kernel kernel,
        [FromBody] UserRequestDto prompt,
        [FromQuery] string? modelName,
        CancellationToken connectionToken
    )
    {
        return GenerateAnswerAsync(
            context,
            kernel,
            prompt.Prompt,
            modelName ?? "qwen",
            connectionToken
        );
    }

    private static async Task GenerateAnswerAsync(
        HttpContext context,
        Kernel kernel,
        string userPrompt,
        string modelName,
        CancellationToken connectionToken
    )
    {
        context.Response.ContentType = "text/x-ndjson; charset=utf-8";

        IChatCompletionService? service = null;

        try
        {
            service = kernel.GetRequiredService<IChatCompletionService>(modelName);
        }
        catch (KernelException)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.SendDtoAsync(SystemMessageDto.ModelDoesNotExist, connectionToken);
            return;
        }


        
        await context.SendDtoAsync(SystemMessageDto.RequestStarted, connectionToken);


        StringBuilder jsonBuilder = new();


        CancellationTokenSource timeoutTokenSource = new();


        timeoutTokenSource.CancelAfter(TimeSpan.FromSeconds(30)); // ожидание пока LLM проснется

        CancellationTokenSource linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            connectionToken,
            timeoutTokenSource.Token
        );
        ChatHistory history = new();


        history.AddSystemMessage(SystemPrompt);
        history.AddUserMessage(userPrompt);

        IAsyncEnumerable<StreamingChatMessageContent> result = service.GetStreamingChatMessageContentsAsync(
            chatHistory: history,
            executionSettings: GetExecutionSettings(modelName),
            cancellationToken: linkedTokenSource.Token
        );


        try
        {
            await foreach (StreamingKernelContent chunk in result.WithCancellation(linkedTokenSource.Token))
            {
                string chunkText = chunk.ToString();
                timeoutTokenSource.CancelAfter(TimeSpan.FromSeconds(3));

                await context.SendDtoAsync(new ChunkDto(chunkText), linkedTokenSource.Token);
                jsonBuilder.Append(chunkText);
 
                timeoutTokenSource.CancelAfter(TimeSpan.FromSeconds(10));
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
       
        BehaviourModelDto? behaviourModel = BehaviourModelDto.FromString(jsonBuilder.ToString());
        
        
        await context.SendDtoAsync(behaviourModel, connectionToken);

        await context.SendDtoAsync(SystemMessageDto.Finished, connectionToken);
    }
}
