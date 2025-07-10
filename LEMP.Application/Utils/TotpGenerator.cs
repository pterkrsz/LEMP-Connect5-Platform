using System.Security.Cryptography;
using System.Text;

namespace LEMP.Application.Utils;

public static class TotpGenerator
{
    public static bool Verify(string secret, string code, int window = 1)
    {
        for (int i = -window; i <= window; i++)
        {
            if (Generate(secret, i) == code)
                return true;
        }
        return false;
    }

    private static string Generate(string secret, int offset)
    {
        var timestep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30 + offset;
        var data = BitConverter.GetBytes(timestep);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(data);

        var key = Encoding.ASCII.GetBytes(secret);
        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(data);
        int start = hash[^1] & 0x0F;
        int binary = ((hash[start] & 0x7F) << 24)
                    | (hash[start + 1] << 16)
                    | (hash[start + 2] << 8)
                    | (hash[start + 3]);
        int otp = binary % 1_000_000;
        return otp.ToString("D6");
    }
}
