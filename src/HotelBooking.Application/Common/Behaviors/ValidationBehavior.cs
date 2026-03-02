using FluentValidation;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResult, IValidationFailureFactory<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = validationResults
            .SelectMany(r => r.Errors)
            .Where(e => e is not null)
            .GroupBy(e => new { e.PropertyName, e.ErrorMessage }) // إزالة التكرار
            .Select(g => Error.Validation(g.Key.PropertyName, g.Key.ErrorMessage))
            .ToArray();

        if (errors.Length == 0)
            return await next();

        return TResponse.FromValidationErrors(errors);
    }
}