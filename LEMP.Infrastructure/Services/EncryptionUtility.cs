using System.Security.Cryptography;
using System.Text;

namespace LEMP.Infrastructure.Services;

public static class EncryptionUtility
{
    public static string Encrypt(string plainText, string key)
    {
        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        aes.IV = new byte[16];
        using var encryptor = aes.CreateEncryptor();
        var inputBytes = Encoding.UTF8.GetBytes(plainText);
        var encrypted = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
        return Convert.ToBase64String(encrypted);
    }

    public static string Decrypt(string cipherText, string key)
    {
        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        aes.IV = new byte[16];
        using var decryptor = aes.CreateDecryptor();
        var inputBytes = Convert.FromBase64String(cipherText);
        var decrypted = decryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
        return Encoding.UTF8.GetString(decrypted);
    }
}
