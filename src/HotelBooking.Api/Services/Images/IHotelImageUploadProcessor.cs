using Microsoft.AspNetCore.Http;

namespace HotelBooking.Api.Services.Images;

public interface IHotelImageUploadProcessor
{
    Task<StoredHotelImageFile> ProcessAndStoreAsync(Guid hotelId, IFormFile file, CancellationToken ct = default);
    void TryDelete(string absolutePath);
}

public sealed record StoredHotelImageFile(
    string RelativeUrl,
    string AbsolutePath,
    string ContentType,
    long SizeBytes);