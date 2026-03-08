namespace HotelBooking.Application.Common.Interfaces;

public interface IEmbeddingService
{
    Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken ct);

    Task<IReadOnlyList<float[]?>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct);

    int Dimensions { get; }

    Task<bool> IsAvailableAsync(CancellationToken ct);
}