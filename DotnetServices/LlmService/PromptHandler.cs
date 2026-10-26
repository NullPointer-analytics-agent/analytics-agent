using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SharedClasses.ResponseDto;
using System.Runtime.CompilerServices;
using System.Text;
using LlmService.Exceptions;

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
    public async IAsyncEnumerable<ResponseDtoBase> GenerateAnswerAsync(
        string userPrompt,
        PromptParameters promptParameters,
        IChatCompletionService completionService,
        [EnumeratorCancellation] CancellationToken connectionToken = default
    )
    { 
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

        StringBuilder jsonBuilder = new();

        timeoutTokenSource.CancelAfter(TimeSpan.FromSeconds(30));

        while (true)
        {
            try
            {
                if (!await resultEnumerator.MoveNextAsync()) break;
            }
            catch (OperationCanceledException)
            {
                if (!connectionToken.IsCancellationRequested)
                {
                    throw new LlmTimeoutException();
                }
                else throw;
            }

            string chunkText = resultEnumerator.Current.ToString();

            jsonBuilder.Append(chunkText);

            timeoutTokenSource.CancelAfter(TimeSpan.FromSeconds(10));

            yield return new ChunkDto(chunkText);

            if (timeoutTokenSource.IsCancellationRequested)
            {
                throw new IterationTimeoutException();
            }

            timeoutTokenSource.CancelAfter(TimeSpan.FromSeconds(3));
        }

        BehaviourModelDto? behaviourModel = BehaviourModelDto.FromString(jsonBuilder.ToString());
        //Todo: advanced validation
        if (behaviourModel == null) throw new InvalidBehaviourModelException();
        yield return behaviourModel;
    }
}
