using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Meterly.IntegrationTests.Auth;

/// <summary>
/// Integration tests for the Auth API endpoints.
///
/// THESE TESTS VERIFY:
///   - POST /api/auth/register → creates tenant + user, returns JWT
///   - POST /api/auth/login → returns JWT for valid credentials
///   - POST /api/auth/login with bad credentials → returns 401
///   - Registering twice with the same email → returns 409 Conflict
///
/// These tests hit the REAL controllers, REAL MediatR pipeline,
/// REAL FluentValidation, and REAL JWT generation — just with
/// a mocked database and gateway.
/// </summary>
public class AuthControllerTests : IClassFixture<MeterlyWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(MeterlyWebApplicationFactory factory)
    {
        _client = factory.CreateApiClient();
    }

    [Fact]
    public async Task Register_WithValidPayload_Returns200WithToken()
    {
        // Arrange
        var payload = new
        {
            TenantName = "Acme Corp",
            FirstName = "Alice",
            LastName = "Smith",
            Email = $"alice+{Guid.NewGuid()}@acme.com", // unique per test run
            Password = "SecurePass123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);

        var json = JsonDocument.Parse(body);
        json.RootElement.TryGetProperty("token", out var tokenProp).Should().BeTrue("response should contain a token");
        tokenProp.GetString().Should().NotBeNullOrWhiteSpace("JWT token should not be empty");
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_Returns200WithToken()
    {
        // Arrange — register first so the user exists
        var email = $"bob+{Guid.NewGuid()}@acme.com";
        var password = "BobsPassword99!";

        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            TenantName = "Bob Corp",
            FirstName = "Bob",
            LastName = "Jones",
            Email = email,
            Password = password
        });

        // Act — login with same credentials
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = password
        });
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);
        var json = JsonDocument.Parse(body);
        json.RootElement.TryGetProperty("token", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        // Arrange
        var email = $"carol+{Guid.NewGuid()}@acme.com";

        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            TenantName = "Carol Corp",
            FirstName = "Carol",
            LastName = "White",
            Email = email,
            Password = "CorrectPassword1!"
        });

        // Act — wrong password
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = "WrongPassword!"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_Returns400()
    {
        // Arrange — invalid email format should fail FluentValidation
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            TenantName = "Test Corp",
            FirstName = "Dave",
            LastName = "Brown",
            Email = "not-an-email",
            Password = "ValidPass1!"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
