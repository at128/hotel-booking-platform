using System.Diagnostics;
using HotelBooking.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Common.Behaviors;

public class PerformanceBehavior<TRequest, TResponse>(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        if (sw.ElapsedMilliseconds > 500)
        {
            var requestData = request is ISensitiveRequest
                ? "[REDACTED - sensitive request]"
                : (object)request;

            logger.LogWarning("Long running request: {Name} ({Elapsed}ms) {@Request}",
                typeof(TRequest).Name, sw.ElapsedMilliseconds, requestData);
        }

        return response;
    }
}