namespace PasswordManager.DTOs.Auth;

public record LoginRequest(string Email, string MasterPassword);