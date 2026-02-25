using HotelBooking.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Common.Behaviors;

public class UnhandledExceptionBehavior<TRequest, TResponse>(
    ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            var requestData = request is ISensitiveRequest
                ? "[REDACTED - sensitive request]"
                : (object)request;

            logger.LogError(ex, "Unhandled exception for {Name}: {@Request}",
                typeof(TRequest).Name, requestData);
            throw;
        }
    }
}