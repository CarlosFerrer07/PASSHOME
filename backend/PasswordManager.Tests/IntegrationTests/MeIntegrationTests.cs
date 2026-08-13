using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PasswordManager.DTOs.Auth;
using PasswordManager.Tests.Fixtures;


namespace PasswordManager.Tests.IntegrationTests;

public class MeIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    public MeIntegrationTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }
    [Fact]
    public async Task Me_WithValidToken_shouldReturnUserProfile()
    {
        await _client.PostAsJsonAsync("api/auth/register", 
            new RegisterRequest("me@test.com", "Password123!"));

        var login = await _client.PostAsJsonAsync("api/auth/login",
            new LoginRequest("me@test.com", "Password123!"));
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.Token);

        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var me = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        me!.Email.Should().Be("me@test.com");
    }
}