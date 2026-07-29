namespace PasswordManager.DTOs.Auth;

public record AuthResponse(string Token, DateTime Expiration);