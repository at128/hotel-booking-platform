namespace HotelBooking.Contracts.Admin;

public sealed record ImageDto(Guid Id, string Url, string? Caption, int SortOrder);

public sealed record UpdateImageRequest(string? Caption, int SortOrder);