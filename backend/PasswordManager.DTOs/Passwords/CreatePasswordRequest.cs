namespace PasswordManager.DTOs.Passwords;

public record CreatePasswordRequest(
    string Title,
    string? Username,
    string Password,
    string? Url,
    int? CategoryId,
    string? Notes);