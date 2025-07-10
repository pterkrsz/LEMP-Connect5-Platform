namespace LEMP.Application.Interfaces;

public interface ITwoFactorService
{
    Task<string?> GetSecretAsync(string username);
    Task SetSecretAsync(string username, string secret);
}
