using Asp.Versioning;
using HotelBooking.Domain.Common.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HotelBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]

public abstract class ApiController : ControllerBase
{
    protected ActionResult Problem(List<Error> errors)
    {
        if (errors.Count == 0)
            return Problem();

        if (errors.All(e => e.Type == ErrorKind.Validation))
            return ValidationProblem(errors);

        return Problem(errors[0]);
    }

    private ActionResult Problem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorKind.Validation => StatusCodes.Status400BadRequest,
            ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
            ErrorKind.NotFound => StatusCodes.Status404NotFound,
            ErrorKind.Conflict => StatusCodes.Status409Conflict,
            ErrorKind.Failure => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError,
        };

        return Problem(
            statusCode: statusCode,
            title: error.Code,
            detail: error.Description);
    }

    private ActionResult ValidationProblem(List<Error> errors)
    {
        var modelState = new ModelStateDictionary();
        foreach (var error in errors)
            modelState.AddModelError(error.Code, error.Description);

        return ValidationProblem(modelState);
    }
}
