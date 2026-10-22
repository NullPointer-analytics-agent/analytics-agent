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


    private static async Task SendDtoAsync(this HttpContext context, object? dto, CancellationToken token)
    {
        if (dto == null) return;
        token.ThrowIfCancellationRequested();
        await context.Response.WriteAsync($"{JsonSerializer.Serialize(dto, serializerOptions)}\n", token);
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
        string prompt,
        string modelName,
        CancellationToken connectionToken
    )
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

