using LEMP.Application.Utils;
using NUnit.Framework;
using System.Security.Cryptography;
using System.Text;

namespace LEMP.Test;

public class TotpGeneratorTests
{
    private static string GenerateCode(string secret, int offset = 0)
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

    [Test]
    public void VerifyReturnsTrueForValidCode()
    {
        const string secret = "TESTSECRET";
        var code = GenerateCode(secret);
        Assert.That(TotpGenerator.Verify(secret, code), Is.True);
    }

    [Test]
    public void VerifyReturnsFalseForInvalidCode()
    {
        const string secret = "TESTSECRET";
        var code = GenerateCode(secret, 2); // offset outside default window
        Assert.That(TotpGenerator.Verify(secret, code), Is.False);
    }
}
