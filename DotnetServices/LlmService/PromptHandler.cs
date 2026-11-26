using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SharedClasses.ResponseDto;
using System.Runtime.CompilerServices;
using System.Text;
using LlmService.Exceptions;
using SharedClasses.ResponseDto.BehaviourModel;

namespace LlmService;


public interface IPromptHandler
{
    IAsyncEnumerable<ResponseDtoBase> GenerateAnswerAsync(
        string userPrompt,
        PromptParameters promptParameters,
        CancellationToken connectionToken = default
    );
}


public class PromptHandler (IChatCompletionService completionService): IPromptHandler 
{
    private readonly IChatCompletionService _completionService = completionService;


    public async IAsyncEnumerable<ResponseDtoBase> GenerateAnswerAsync(
        string userPrompt,
        PromptParameters promptParameters,
        [EnumeratorCancellation] CancellationToken connectionToken = default
    )
    { 

        ChatHistory history = [];
        promptParameters.AddSystemPromptsToHistory(history);
        history.AddUserMessage(userPrompt);

        await using IAsyncEnumerator<StreamingChatMessageContent> resultEnumerator = _completionService.GetStreamingChatMessageContentsAsync(
            chatHistory: history,
            executionSettings: promptParameters.PromptExecutionSettings,
            cancellationToken: connectionToken
        ).GetAsyncEnumerator(connectionToken);

        StringBuilder jsonBuilder = new();


        while (true)
        {
            if (!await resultEnumerator.MoveNextAsync()) break;

            string chunkText = resultEnumerator.Current.ToString();
            jsonBuilder.Append(chunkText);

            yield return new ChunkDto(chunkText);
        }   

        BehaviourModelDto? behaviourModel = BehaviourModelDto.FromString(jsonBuilder.ToString());
        //Todo: advanced validation
        if (behaviourModel == null) throw new InvalidBehaviourModelException();
        yield return behaviourModel;
    }
}
