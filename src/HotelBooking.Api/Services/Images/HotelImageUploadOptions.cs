namespace HotelBooking.Api.Services.Images;

public sealed class HotelImageUploadOptions
{
    public const string SectionName = "Uploads:HotelImages";

    public long MaxFileBytes { get; init; } = 5 * 1024 * 1024; // 5 MB
    public int MaxWidth { get; init; } = 4096;
    public int MaxHeight { get; init; } = 4096;
    public long MaxPixels { get; init; } = 16_000_000; // 16MP
    public int JpegQuality { get; init; } = 85;
    public int MaxImagesPerHotel { get; init; } = 100;
}