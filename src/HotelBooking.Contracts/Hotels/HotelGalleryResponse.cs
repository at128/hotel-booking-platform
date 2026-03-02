namespace HotelBooking.Contracts.Hotels;

public sealed record ImageDto(
    Guid Id,
    string Url,
    string? Caption,
    int SortOrder,
    string EntityType);

public sealed record HotelGalleryResponse(
    Guid HotelId,
    List<ImageDto> Images);