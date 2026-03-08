namespace HotelBooking.Contracts.Admin;

public sealed record LinkServiceRequest(Guid ServiceId, decimal Price, bool IsFree);