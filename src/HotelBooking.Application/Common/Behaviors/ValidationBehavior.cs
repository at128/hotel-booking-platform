using FluentValidation;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IValidator<TRequest>? validator = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResult
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (validator is null) return await next();

        var result = await validator.ValidateAsync(request, cancellationToken);
        if (result.IsValid) return await next();

        var errors = result.Errors
            .ConvertAll(e => Error.Validation(e.PropertyName, e.ErrorMessage));

        return (dynamic)errors;
    }
}
