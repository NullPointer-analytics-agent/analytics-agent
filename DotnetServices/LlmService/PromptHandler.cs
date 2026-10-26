using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SharedClasses.ResponseDto;
using System.Runtime.CompilerServices;
using System.Text;


namespace LlmService;


public interface IPromptHandler
{
    IAsyncEnumerable<ResponseDtoBase> GenerateAnswerAsync(
        string userPrompt,
        PromptParameters promptParameters,
        IChatCompletionService chatCompletionService,
        CancellationToken connectionToken = default
    );
}

public class PromptHandler: IPromptHandler
{
    private enum ExecutionStatus
    {
        InProgress,
        IterationTimeout,
        LlmTimeout,
        ConnectionCancellation,
        Finished
    }

    public async IAsyncEnumerable<ResponseDtoBase> GenerateAnswerAsync(
        string userPrompt,
        PromptParameters promptParameters,
        IChatCompletionService completionService,
        [EnumeratorCancellation] CancellationToken connectionToken = default
    )
    { 

        yield return new SystemStatusDto(SystemStatus.RequestStarted);


        StringBuilder jsonBuilder = new();


        using CancellationTokenSource timeoutTokenSource = new();
        using CancellationTokenSource linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            connectionToken,
            timeoutTokenSource.Token
        );


        ChatHistory history = [];
        history.AddSystemMessage(promptParameters.SystemPrompt);
        history.AddUserMessage(userPrompt);



        await using IAsyncEnumerator<StreamingChatMessageContent> resultEnumerator = completionService.GetStreamingChatMessageContentsAsync(
            chatHistory: history,
            executionSettings: promptParameters.PromptExecutionSettings,
            cancellationToken: linkedTokenSource.Token
        ).GetAsyncEnumerator(linkedTokenSource.Token);


        timeoutTokenSource.CancelAfter(TimeSpan.FromSeconds(30));

        ExecutionStatus status = ExecutionStatus.InProgress;


        while (status == ExecutionStatus.InProgress)
        {
            try
            {
                bool inProgress = await resultEnumerator.MoveNextAsync();
                if(!inProgress) status = ExecutionStatus.Finished;
            }
            catch (OperationCanceledException)
            {
                if (!connectionToken.IsCancellationRequested)
                {
                    status = ExecutionStatus.LlmTimeout;
                    break;
                }
                else throw;
            }
            if (status != ExecutionStatus.InProgress) break;

            string chunkText = resultEnumerator.Current.ToString();

            jsonBuilder.Append(chunkText);

            timeoutTokenSource.CancelAfter(TimeSpan.FromSeconds(10));

            yield return new ChunkDto(chunkText);

            if (timeoutTokenSource.IsCancellationRequested)
            {
                status = ExecutionStatus.IterationTimeout;
                break;
            }

            timeoutTokenSource.CancelAfter(TimeSpan.FromSeconds(3));
        }

        if(status == ExecutionStatus.IterationTimeout)
        {
            yield return new SystemStatusDto(SystemStatus.IterationTimeout);
            yield break;
        }

        if (status == ExecutionStatus.LlmTimeout)
        {
            yield return new SystemStatusDto(SystemStatus.LlmTimeout);
            yield break;
        }

        if (status == ExecutionStatus.Finished)
        {
            BehaviourModelDto? behaviourModel = BehaviourModelDto.FromString(jsonBuilder.ToString());

            if (behaviourModel != null) yield return behaviourModel;
            else yield return new SystemStatusDto(SystemStatus.BehaviourModelSerializationError);

            yield return new SystemStatusDto(SystemStatus.Finished);
            yield break;
        }
    }
}
