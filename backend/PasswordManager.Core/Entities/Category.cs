namespace PasswordManager.Core.Entities;

public class Category
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    
    public User User { get; set; } = null!;
    public ICollection<PasswordEntry> Passwords { get; set; } = [];
}