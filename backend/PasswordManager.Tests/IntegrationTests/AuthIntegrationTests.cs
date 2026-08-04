using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PasswordManager.DTOs.Auth;
using PasswordManager.Tests.Fixtures;

namespace PasswordManager.Tests.IntegrationTests;

public class AuthIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuthIntegrationTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturnOk()
    {
        var request = new RegisterRequest("newuser@test.com", "Pass123!");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("newuser@test.com");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnConflict()
    {
        var request = new RegisterRequest("duplicate@test.com", "Pass123!");
        await _client.PostAsJsonAsync("/api/auth/register", request);

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        var registerRequest = new RegisterRequest("logintest@test.com", "Pass123!");
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest("logintest@test.com", "Pass123!");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.Token.Should().NotBeEmpty();
        body.Expiration.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturnUnauthorized()
    {
        var registerRequest = new RegisterRequest("wrongpwd@test.com", "Pass123!");
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest("wrongpwd@test.com", "WrongPassword!");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ShouldReturnUnauthorized()
    {
        var loginRequest = new LoginRequest("nonexistent@test.com", "Pass123!");

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ShouldReturnValidJwtToken()
    {
        var registerRequest = new RegisterRequest("jwttest@test.com", "Pass123!");
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest("jwttest@test.com", "Pass123!");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        var parts = body!.Token.Split('.');
        parts.Should().HaveCount(3);
    }
}
