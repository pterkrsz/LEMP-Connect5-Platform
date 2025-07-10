using LEMP.Application.Interfaces;
using LEMP.Domain;
using LEMP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LEMP.Infrastructure.Services;

public class EfTwoFactorService : ITwoFactorService
{
    private readonly MeasurementDbContext _db;
    private readonly string _key;

    public EfTwoFactorService(MeasurementDbContext db, IConfiguration config)
    {
        _db = db;
        _key = config["EncryptionKey"] ?? "default-secret-key-please-change";
    }

    public async Task<string?> GetSecretAsync(string username)
    {
        var entry = await _db.TwoFactorSecrets.SingleOrDefaultAsync(e => e.Username == username);
        if (entry == null) return null;
        return EncryptionUtility.Decrypt(entry.EncryptedSecret, _key);
    }

    public async Task SetSecretAsync(string username, string secret)
    {
        var encrypted = EncryptionUtility.Encrypt(secret, _key);
        var entry = await _db.TwoFactorSecrets.SingleOrDefaultAsync(e => e.Username == username);
        if (entry == null)
        {
            _db.TwoFactorSecrets.Add(new TwoFactorSecret { Username = username, EncryptedSecret = encrypted });
        }
        else
        {
            entry.EncryptedSecret = encrypted;
        }
        await _db.SaveChangesAsync();
    }
}
