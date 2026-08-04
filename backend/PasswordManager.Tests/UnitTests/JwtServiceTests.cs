using FluentAssertions;
using Microsoft.Extensions.Configuration;
using PasswordManager.Infrastructure.Services;

namespace PasswordManager.Tests.UnitTests;

public class JwtServiceTests
{
    private readonly JwtService _sut;

    public JwtServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:Key", "SuperSecretKeyForTestingPurposes123456!" },
                { "Jwt:Issuer", "TestIssuer" },
                { "Jwt:Audience", "TestAudience" }
            })
            .Build();

        _sut = new JwtService(config);
    }

    [Fact]
    public void GenerateToken_ShouldReturnNonEmptyToken()
    {
        var token = _sut.GenerateToken(1, "test@example.com");

        token.Should().NotBeEmpty();
    }

    [Fact]
    public void GenerateToken_ShouldReturnJwtFormatToken()
    {
        var token = _sut.GenerateToken(1, "test@example.com");

        token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void GenerateToken_ShouldBeDecodable()
    {
        var token = _sut.GenerateToken(42, "user@test.com");

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c =>
            c.Type == System.Security.Claims.ClaimTypes.NameIdentifier && c.Value == "42");
        jwtToken.Claims.Should().Contain(c =>
            c.Type == System.Security.Claims.ClaimTypes.Email && c.Value == "user@test.com");
    }

    [Fact]
    public void GenerateToken_ShouldHaveCorrectIssuerAndAudience()
    {
        var token = _sut.GenerateToken(1, "test@example.com");

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be("TestIssuer");
        jwtToken.Audiences.Should().Contain("TestAudience");
    }

    [Fact]
    public void GenerateToken_ShouldHaveExpiration()
    {
        var token = _sut.GenerateToken(1, "test@example.com");

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.ValidTo.Should().BeAfter(DateTime.UtcNow);
        jwtToken.ValidTo.Should().BeBefore(DateTime.UtcNow.AddHours(25));
    }

    [Fact]
    public void GenerateToken_ShouldHaveUniqueJtiClaim()
    {
        var token1 = _sut.GenerateToken(1, "test@example.com");
        var token2 = _sut.GenerateToken(1, "test@example.com");

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jti1 = handler.ReadJwtToken(token1).Claims.First(c => c.Type == "jti").Value;
        var jti2 = handler.ReadJwtToken(token2).Claims.First(c => c.Type == "jti").Value;

        jti1.Should().NotBe(jti2);
    }
}
