namespace PasswordManager.DTOs.Passwords;

public record UpdatePasswordRequest(
    string Title,
    string? Username,
    string? Password,
    string? Url,
    int? CategoryId,
    string? Notes);