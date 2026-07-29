namespace PasswordManager.DTOs.Passwords;

public record GeneratePasswordRequest(
    int Length = 16,
    bool IncludeUpper = true,
    bool IncludeLower = true,
    bool IncludeNumbers = true,
    bool IncludeSymbols = true);