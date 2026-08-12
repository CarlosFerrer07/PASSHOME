using FluentAssertions;
using PasswordManager.Infrastructure.Services;

namespace PasswordManager.Tests.UnitTests;

public class GeneratePasswordTheoryTests
{
    private readonly EncryptionService _sut = new();

    [Theory]
    [InlineData(10, true, false, false, false, "^[A-Z]+$")]
    [InlineData(8, false, true, false, false, "^[a-z]+$")]
    [InlineData(16, false, false, true, false, "^[0-9]+$")]
    [InlineData(12, false, false, false, true, "^[!@#$%^&*()_+\\-=\\[\\]{}|;:,.<>?]+$")]
    [InlineData(20, true, true, false, true, "^[A-Za-z!@#$%^&*()_+\\-=\\[\\]{}|;:,.<>?]+$")]
    [InlineData(64, true, true, true, true, "^[A-Za-z0-9!@#$%^&*()_+\\-=\\[\\]{}|;:,.<>?]+$")]
    [InlineData(1, true, true, true, true, "^[A-Za-z0-9!@#$%^&*()_+\\-=\\[\\]{}|;:,.<>?]+$")]
    [InlineData(5, false, false, false, false, "^[a-z]+$")]
    public void GeneratePassword_ShouldMatchRequestedCharset(int length, bool includeUpper, bool includeLower, bool includeNumbers, bool includeSymbols, string pattern)
    {
        // Act
        var password = _sut.GeneratePassword(length, includeUpper, includeLower, includeNumbers, includeSymbols);
        // Assert
        password.Should().HaveLength(length);
        password.Should().MatchRegex(pattern);
    }
}

