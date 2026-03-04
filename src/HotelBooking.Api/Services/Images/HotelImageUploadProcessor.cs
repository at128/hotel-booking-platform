using HotelBooking.Domain.Common.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace HotelBooking.Api.Services.Images;

public sealed class HotelImageUploadProcessor(
    IWebHostEnvironment env,
    IOptions<HotelImageUploadOptions> options)
    : IHotelImageUploadProcessor
{
    private readonly HotelImageUploadOptions _options = options.Value;

    public async Task<StoredHotelImageFile> ProcessAndStoreAsync(
        Guid hotelId,
        IFormFile file,
        CancellationToken ct = default)
    {
        if (file is null || file.Length <= 0)
            throw new ImageUploadValidationException("Image file is required.");

        if (file.Length > _options.MaxFileBytes)
            throw new ImageUploadValidationException(
                $"Image file size must be <= {_options.MaxFileBytes / (1024 * 1024)} MB.",
                StatusCodes.Status413PayloadTooLarge);

        // UX check (not trust boundary)
        var originalExtension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(originalExtension) ||
            !HotelBookingConstants.Image.AllowedExtensions.Contains(originalExtension, StringComparer.OrdinalIgnoreCase))
        {
            throw new ImageUploadValidationException("Unsupported image extension.");
        }

        await using var source = file.OpenReadStream();

        // Read header (magic bytes)
        var header = new byte[32];
        var read = await source.ReadAsync(header.AsMemory(0, header.Length), ct);
        source.Position = 0;

        var detected = DetectImageType(header.AsSpan(0, read));
        if (detected is null)
            throw new ImageUploadValidationException("File is not a supported image.");

        // Optional cross-check (client-provided, not trusted)
        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            var normalizedContentType = file.ContentType.Trim();
            var detectedMime = detected.Value switch
            {
                DetectedImageType.Jpeg => "image/jpeg",
                DetectedImageType.Png => "image/png",
                DetectedImageType.Webp => "image/webp",
                _ => "application/octet-stream"
            };

            // If client sends a content type and it conflicts, reject
            if (!normalizedContentType.Equals(detectedMime, StringComparison.OrdinalIgnoreCase))
            {
                throw new ImageUploadValidationException("Image content type does not match file signature.");
            }
        }

        // Decode image => proves it's a real parsable image (not just spoofed bytes)
        using var image = await Image.LoadAsync(source, ct);

        // Normalize orientation before dimension checks
        image.Mutate(x => x.AutoOrient());

        if (image.Width <= 0 || image.Height <= 0)
            throw new ImageUploadValidationException("Invalid image dimensions.");

        if (image.Width > _options.MaxWidth || image.Height > _options.MaxHeight)
        {
            throw new ImageUploadValidationException(
                $"Image dimensions exceed limit ({_options.MaxWidth}x{_options.MaxHeight}).");
        }

        var pixels = (long)image.Width * image.Height;
        if (pixels > _options.MaxPixels)
            throw new ImageUploadValidationException("Image pixel count exceeds allowed limit.");

        // Strip metadata (privacy + attack surface reduction)
        image.Metadata.ExifProfile = null;
        image.Metadata.IccProfile = null;
        image.Metadata.XmpProfile = null;

        // Standardize output format => JPEG (sanitization by re-encoding)
        var webRoot = env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
            webRoot = Path.Combine(env.ContentRootPath, "wwwroot");

        var hotelFolder = Path.Combine(webRoot, "images", "hotels", hotelId.ToString("N"));
        Directory.CreateDirectory(hotelFolder);

        var fileName = $"{Guid.CreateVersion7():N}.jpg";
        var finalPath = Path.Combine(hotelFolder, fileName);
        var tempPath = Path.Combine(hotelFolder, $"{Guid.CreateVersion7():N}.tmp");

        try
        {
            await using (var outStream = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true))
            {
                var encoder = new JpegEncoder { Quality = _options.JpegQuality };
                await image.SaveAsJpegAsync(outStream, encoder, ct);
                await outStream.FlushAsync(ct);
            }

            File.Move(tempPath, finalPath, overwrite: false);

            var info = new FileInfo(finalPath);
            if (!info.Exists || info.Length == 0)
                throw new IOException("Failed to persist uploaded image.");

            var relativeUrl = $"/images/hotels/{hotelId:N}/{fileName}";
            return new StoredHotelImageFile(
                RelativeUrl: relativeUrl,
                AbsolutePath: finalPath,
                ContentType: "image/jpeg",
                SizeBytes: info.Length);
        }
        catch
        {
            TryDelete(tempPath);
            TryDelete(finalPath);
            throw;
        }
    }

    public void TryDelete(string absolutePath)
    {
        if (string.IsNullOrWhiteSpace(absolutePath))
            return;

        try
        {
            if (File.Exists(absolutePath))
                File.Delete(absolutePath);
        }
        catch
        {
            // intentionally swallow during cleanup
        }
    }

    private enum DetectedImageType
    {
        Jpeg,
        Png,
        Webp
    }

    private static DetectedImageType? DetectImageType(ReadOnlySpan<byte> header)
    {
        // JPEG: FF D8 FF
        if (header.Length >= 3 &&
            header[0] == 0xFF &&
            header[1] == 0xD8 &&
            header[2] == 0xFF)
        {
            return DetectedImageType.Jpeg;
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (header.Length >= 8 &&
            header[0] == 0x89 &&
            header[1] == 0x50 &&
            header[2] == 0x4E &&
            header[3] == 0x47 &&
            header[4] == 0x0D &&
            header[5] == 0x0A &&
            header[6] == 0x1A &&
            header[7] == 0x0A)
        {
            return DetectedImageType.Png;
        }

        // WEBP: "RIFF" .... "WEBP"
        if (header.Length >= 12 &&
            header[0] == (byte)'R' &&
            header[1] == (byte)'I' &&
            header[2] == (byte)'F' &&
            header[3] == (byte)'F' &&
            header[8] == (byte)'W' &&
            header[9] == (byte)'E' &&
            header[10] == (byte)'B' &&
            header[11] == (byte)'P')
        {
            return DetectedImageType.Webp;
        }

        return null;
    }
}