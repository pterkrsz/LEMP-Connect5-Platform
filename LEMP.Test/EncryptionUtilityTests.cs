using LEMP.Infrastructure.Services;
using NUnit.Framework;

namespace LEMP.Test;

public class EncryptionUtilityTests
{
    [Test]
    public void EncryptAndDecryptRoundTrip()
    {
        const string text = "secret-data";
        const string key = "encryption-key";

        var encrypted = EncryptionUtility.Encrypt(text, key);
        Assert.That(encrypted, Is.Not.EqualTo(text));

        var decrypted = EncryptionUtility.Decrypt(encrypted, key);
        Assert.That(decrypted, Is.EqualTo(text));
    }
}
