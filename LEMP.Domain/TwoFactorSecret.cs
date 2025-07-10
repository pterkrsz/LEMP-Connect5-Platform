namespace LEMP.Domain;

public class TwoFactorSecret
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string EncryptedSecret { get; set; } = string.Empty;
}
