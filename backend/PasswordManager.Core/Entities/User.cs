namespace PasswordManager.Core.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public byte[] MasterPasswordHash { get; set; } = [];
    public byte[] Salt { get; set; } = [];
    public byte[] EncryptedDataKey { get; set; } = [];
    public byte[] DataKeyIV { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<PasswordEntry> Passwords { get; set; } = [];
    public ICollection<Category> Categories { get; set; } = [];
}