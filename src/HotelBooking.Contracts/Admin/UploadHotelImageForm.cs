using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HotelBooking.Api.Contracts.Admin;

public sealed class UploadHotelImageForm
{
    [Required]
    public IFormFile Image { get; set; } = null!;

    [MaxLength(200)]
    public string? Caption { get; set; }

    [Range(0, int.MaxValue)]
    public int? SortOrder { get; set; }
}