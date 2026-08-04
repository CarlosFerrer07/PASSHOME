using System.Text;
using FluentAssertions;
using PasswordManager.Infrastructure.Services;

namespace PasswordManager.Tests.UnitTests;

public class EncryptionServiceTests
{
    private readonly EncryptionService _sut = new();

    [Fact]
    public void Encrypt_ShouldReturnNonEmptyCipherText()
    {
        var key = _sut.GenerateKey();
        var plainText = "Hello, World!";

        var cipherText = _sut.Encrypt(plainText, key, out var iv);

        cipherText.Should().NotBeEmpty();
        iv.Should().NotBeEmpty();
    }

    [Fact]
    public void Encrypt_ShouldReturnDifferentCipherTextOnSecondCall()
    {
        var key = _sut.GenerateKey();
        var plainText = "Hello, World!";

        var cipherText1 = _sut.Encrypt(plainText, key, out _);
        var cipherText2 = _sut.Encrypt(plainText, key, out _);

        cipherText1.Should().NotBeEquivalentTo(cipherText2);
    }

    [Fact]
    public void Encrypt_Decrypt_ShouldReturnOriginalPlainText()
    {
        var key = _sut.GenerateKey();
        var plainText = "MySecretPassword123!";

        var cipherText = _sut.Encrypt(plainText, key, out var iv);
        var decrypted = _sut.Decrypt(cipherText, key, iv);

        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void Encrypt_Decrypt_ShouldWorkWithUnicodeCharacters()
    {
        var key = _sut.GenerateKey();
        var plainText = "Contraseña con tilde ñ y emoji 🔐";

        var cipherText = _sut.Encrypt(plainText, key, out var iv);
        var decrypted = _sut.Decrypt(cipherText, key, iv);

        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void GenerateKey_ShouldReturn32Bytes()
    {
        var key = _sut.GenerateKey();

        key.Should().HaveCount(32);
    }

    [Fact]
    public void GenerateKey_ShouldReturnDifferentKeysOnEachCall()
    {
        var key1 = _sut.GenerateKey();
        var key2 = _sut.GenerateKey();

        key1.Should().NotBeEquivalentTo(key2);
    }

    [Fact]
    public void DeriveKeyFromPassword_ShouldReturn32Bytes()
    {
        var salt = _sut.GenerateSalt();

        var key = _sut.DeriveKeyFromPassword("MyPassword", salt);

        key.Should().HaveCount(32);
    }

    [Fact]
    public void DeriveKeyFromPassword_ShouldReturnSameKeyForSamePasswordAndSalt()
    {
        var salt = _sut.GenerateSalt();

        var key1 = _sut.DeriveKeyFromPassword("MyPassword", salt);
        var key2 = _sut.DeriveKeyFromPassword("MyPassword", salt);

        key1.Should().BeEquivalentTo(key2);
    }

    [Fact]
    public void DeriveKeyFromPassword_ShouldReturnDifferentKeysForDifferentPasswords()
    {
        var salt = _sut.GenerateSalt();

        var key1 = _sut.DeriveKeyFromPassword("Password1", salt);
        var key2 = _sut.DeriveKeyFromPassword("Password2", salt);

        key1.Should().NotBeEquivalentTo(key2);
    }

    [Fact]
    public void HashPassword_ShouldReturnBase64String()
    {
        var salt = _sut.GenerateSalt();

        var hash = _sut.HashPassword("MyPassword", salt);

        hash.Should().NotBeEmpty();
        Action act = () => Convert.FromBase64String(hash);
        act.Should().NotThrow();
    }

    [Fact]
    public void HashPassword_ShouldReturnDifferentHashesForDifferentSalts()
    {
        var salt1 = _sut.GenerateSalt();
        var salt2 = _sut.GenerateSalt();

        var hash1 = _sut.HashPassword("MyPassword", salt1);
        var hash2 = _sut.HashPassword("MyPassword", salt2);

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void VerifyPassword_ShouldReturnTrueForCorrectPassword()
    {
        var salt = _sut.GenerateSalt();
        var hash = Convert.FromBase64String(_sut.HashPassword("MyPassword", salt));

        var result = _sut.VerifyPassword("MyPassword", hash, salt);

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalseForIncorrectPassword()
    {
        var salt = _sut.GenerateSalt();
        var hash = Convert.FromBase64String(_sut.HashPassword("MyPassword", salt));

        var result = _sut.VerifyPassword("WrongPassword", hash, salt);

        result.Should().BeFalse();
    }

    [Fact]
    public void GenerateSalt_ShouldReturn16Bytes()
    {
        var salt = _sut.GenerateSalt();

        salt.Should().HaveCount(16);
    }

    [Fact]
    public void GenerateSalt_ShouldReturnDifferentSaltsOnEachCall()
    {
        var salt1 = _sut.GenerateSalt();
        var salt2 = _sut.GenerateSalt();

        salt1.Should().NotBeEquivalentTo(salt2);
    }

    [Fact]
    public void GeneratePassword_ShouldReturnCorrectLength()
    {
        var password = _sut.GeneratePassword(20, true, true, true, true);

        password.Should().HaveLength(20);
    }

    [Fact]
    public void GeneratePassword_ShouldContainUppercaseWhenRequested()
    {
        var password = _sut.GeneratePassword(100, true, false, false, false);

        password.Should().MatchRegex("^[A-Z]+$");
    }

    [Fact]
    public void GeneratePassword_ShouldContainLowercaseWhenRequested()
    {
        var password = _sut.GeneratePassword(100, false, true, false, false);

        password.Should().MatchRegex("^[a-z]+$");
    }

    [Fact]
    public void GeneratePassword_ShouldContainNumbersWhenRequested()
    {
        var password = _sut.GeneratePassword(100, false, false, true, false);

        password.Should().MatchRegex("^[0-9]+$");
    }

    [Fact]
    public void GeneratePassword_ShouldContainSymbolsWhenRequested()
    {
        var password = _sut.GeneratePassword(100, false, false, false, true);

        password.Should().MatchRegex("^[!@#$%^&*()_+\\-=\\[\\]{}|;:,.<>?]+$");
    }

    [Fact]
    public void GeneratePassword_ShouldFallbackToLowercaseWhenNothingSelected()
    {
        var password = _sut.GeneratePassword(10, false, false, false, false);

        password.Should().HaveLength(10);
        password.Should().MatchRegex("^[a-z]+$");
    }

    [Fact]
    public void Encrypt_Decrypt_ShouldWorkWithEmptyString()
    {
        var key = _sut.GenerateKey();
        var plainText = "";

        var cipherText = _sut.Encrypt(plainText, key, out var iv);
        var decrypted = _sut.Decrypt(cipherText, key, iv);

        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void Encrypt_Decrypt_ShouldWorkWithLongText()
    {
        var key = _sut.GenerateKey();
        var plainText = new string('A', 10_000);

        var cipherText = _sut.Encrypt(plainText, key, out var iv);
        var decrypted = _sut.Decrypt(cipherText, key, iv);

        decrypted.Should().Be(plainText);
    }
}
