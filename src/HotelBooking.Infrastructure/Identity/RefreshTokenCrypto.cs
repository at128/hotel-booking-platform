using System.Security.Cryptography;
using System.Text;

namespace HotelBooking.Infrastructure.Identity;

public static class RefreshTokenCrypto
{
    public static string GenerateRawToken(int tokenBytes = 64)
    {
        var bytes = new byte[tokenBytes];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}