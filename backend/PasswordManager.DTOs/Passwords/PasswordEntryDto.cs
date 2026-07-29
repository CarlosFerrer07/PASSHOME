namespace PasswordManager.DTOs.Passwords;

public record PasswordEntryDto(
    int Id,
    string Title,
    string? Username,
    string? DecryptedPassword,
    string? Url,
    int? CategoryId,
    string? CategoryName,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt);