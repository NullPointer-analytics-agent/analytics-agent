using SharedClasses.ResponseDto;

namespace LlmService.Exceptions;

public class ExceptionMiddleware: IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (InvalidLlmNameException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.SendDtoAsync(new SystemStatusDto(SystemStatus.InvalidLlmName), context.RequestAborted);
        }
        catch (LlmTimeoutException)
        {
            await context.SendDtoAsync(new SystemStatusDto(SystemStatus.LlmTimeout), context.RequestAborted);
        }
        catch (IterationTimeoutException)
        {
            await context.SendDtoAsync(new SystemStatusDto(SystemStatus.IterationTimeout), context.RequestAborted);
        }
        catch (InvalidBehaviourModelException)
        {
            await context.SendDtoAsync(new SystemStatusDto(SystemStatus.BehaviourModelInvalid), context.RequestAborted);
        }
        catch (OperationCanceledException)
        {

        }
    }
}
