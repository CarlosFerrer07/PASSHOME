namespace PasswordManager.Core.Entities;

public class PasswordEntry
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Username { get; set; }
    public byte[] EncryptedPassword { get; set; } = [];
    public byte[] PasswordIV { get; set; } = [];
    public string? Url { get; set; }
    public int? CategoryId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public User User { get; set; } = null!;
    public Category? Category { get; set; }
}