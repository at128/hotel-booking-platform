namespace HotelBooking.Application.Common.Interfaces;

/// <summary>
/// Encapsulates atomic checkout hold operations.
/// Implementations must handle concurrency internally via database transactions.
/// </summary>
public interface ICheckoutHoldRepository
{
    /// <summary>
    /// Atomically checks availability and acquires holds for all requested room types.
    /// Uses SERIALIZABLE transaction to prevent double-booking race conditions.
    /// Returns a list of created hold IDs, or an error if any room type is unavailable.
    /// </summary>
    Task<HoldAcquisitionResult> TryAcquireHoldsAsync(
        Guid userId,
        List<HoldRequest> requests,
        TimeSpan holdDuration,
        CancellationToken ct = default);

    /// <summary>
    /// Releases all holds for a given user (called after successful booking or timeout).
    /// </summary>
    Task ReleaseHoldsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets active (non-released, non-expired) holds for a user.
    /// </summary>
    Task<List<ActiveHoldDto>> GetActiveHoldsAsync(
        Guid userId, CancellationToken ct = default);
}

public sealed record HoldRequest(
    Guid HotelId,
    Guid HotelRoomTypeId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Quantity);

public sealed record HoldAcquisitionResult(
    bool IsSuccess,
    List<Guid> HoldIds,
    DateTimeOffset? ExpiresAtUtc = null,
    string? FailedRoomTypeName = null);

public sealed record ActiveHoldDto(
    Guid Id,
    Guid HotelRoomTypeId,
    string RoomTypeName,
    int Quantity,
    DateTimeOffset ExpiresAtUtc);