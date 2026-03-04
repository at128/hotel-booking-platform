using Microsoft.AspNetCore.Http;

namespace HotelBooking.Api.Services.Images;

public sealed class ImageUploadValidationException : Exception
{
    public int StatusCode { get; }

    public ImageUploadValidationException(
        string message,
        int statusCode = StatusCodes.Status400BadRequest) : base(message)
    {
        StatusCode = statusCode;
    }
}