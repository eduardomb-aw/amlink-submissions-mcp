using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace AmLink.Submissions.Mcp.Tests.Tools;

public class SubmissionApiToolsTests
{
    private const string TestScope = "submission-api";
    private const string OtherScope = "other-api";

    /// <summary>
    /// Helper method to create a valid JWT token for testing.
    /// </summary>
    private static string CreateTestToken(string scopes, DateTime? expirationTime = null)
    {
        var securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("test-secret-key-that-is-long-enough-for-security"));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim("scope", scopes),
            new Claim("sub", "test-user")
        };

        var token = new JwtSecurityToken(
            issuer: "test-issuer",
            audience: "test-audience",
            claims: claims,
            expires: expirationTime ?? DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Helper method to create an invalid (malformed) token.
    /// </summary>
    private static string CreateInvalidToken()
    {
        return "invalid-token-format";
    }

    /// <summary>
    /// Helper method to create a token without scope claim.
    /// </summary>
    private static string CreateTokenWithoutScope(DateTime? expirationTime = null)
    {
        var securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("test-secret-key-that-is-long-enough-for-security"));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim("sub", "test-user")
        };

        var token = new JwtSecurityToken(
            issuer: "test-issuer",
            audience: "test-audience",
            claims: claims,
            expires: expirationTime ?? DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public void JwtTokenHandler_Should_ReadValidToken()
    {
        // Arrange
        var token = CreateTestToken($"{TestScope} other-scope");
        var handler = new JwtSecurityTokenHandler();

        // Act
        var canRead = handler.CanReadToken(token);
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        Assert.True(canRead, "Handler should be able to read valid JWT token");
        Assert.NotNull(jwtToken);
        Assert.Contains(jwtToken.Claims, c => c.Type == "scope");
    }

    [Fact]
    public void JwtTokenHandler_Should_RejectInvalidToken()
    {
        // Arrange
        var token = CreateInvalidToken();
        var handler = new JwtSecurityTokenHandler();

        // Act
        var canRead = handler.CanReadToken(token);

        // Assert
        Assert.False(canRead, "Handler should reject invalid JWT token format");
    }

    [Fact]
    public void JwtToken_Should_ContainExpectedScope()
    {
        // Arrange
        var token = CreateTestToken($"{TestScope} {OtherScope}");
        var handler = new JwtSecurityTokenHandler();

        // Act
        var jwtToken = handler.ReadJwtToken(token);
        var scopeClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "scope");
        var scopes = scopeClaim?.Value.Split(' ') ?? Array.Empty<string>();

        // Assert
        Assert.NotNull(scopeClaim);
        Assert.Contains(TestScope, scopes);
        Assert.Contains(OtherScope, scopes);
    }

    [Fact]
    public void JwtToken_Should_NotContainUnexpectedScope()
    {
        // Arrange
        var token = CreateTestToken(OtherScope);
        var handler = new JwtSecurityTokenHandler();

        // Act
        var jwtToken = handler.ReadJwtToken(token);
        var scopeClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "scope");
        var scopes = scopeClaim?.Value.Split(' ') ?? Array.Empty<string>();

        // Assert
        Assert.NotNull(scopeClaim);
        Assert.DoesNotContain(TestScope, scopes);
        Assert.Contains(OtherScope, scopes);
    }

    [Fact]
    public void JwtToken_Should_DetectExpiredToken()
    {
        // Arrange
        var expiredTime = DateTime.UtcNow.AddHours(-1);
        var token = CreateTestToken(TestScope, expiredTime);
        var handler = new JwtSecurityTokenHandler();

        // Act
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        Assert.True(jwtToken.ValidTo < DateTime.UtcNow, "Token should be expired");
    }

    [Fact]
    public void JwtToken_Should_DetectValidNotExpiredToken()
    {
        // Arrange
        var futureTime = DateTime.UtcNow.AddHours(1);
        var token = CreateTestToken(TestScope, futureTime);
        var handler = new JwtSecurityTokenHandler();

        // Act
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        Assert.True(jwtToken.ValidTo > DateTime.UtcNow, "Token should not be expired");
    }

    [Fact]
    public void JwtToken_Should_HandleMissingScopeClaim()
    {
        // Arrange
        var token = CreateTokenWithoutScope();
        var handler = new JwtSecurityTokenHandler();

        // Act
        var jwtToken = handler.ReadJwtToken(token);
        var scopeClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "scope");

        // Assert
        Assert.Null(scopeClaim);
    }

    [Fact]
    public void JwtToken_Should_HandleMultipleScopes()
    {
        // Arrange
        var scopes = "scope1 scope2 scope3 submission-api scope5";
        var token = CreateTestToken(scopes);
        var handler = new JwtSecurityTokenHandler();

        // Act
        var jwtToken = handler.ReadJwtToken(token);
        var scopeClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "scope");
        var scopeArray = scopeClaim?.Value.Split(' ') ?? Array.Empty<string>();

        // Assert
        Assert.NotNull(scopeClaim);
        Assert.Equal(5, scopeArray.Length);
        Assert.Contains(TestScope, scopeArray);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("not-a-jwt")]
    [InlineData("one.two")]
    public void JwtTokenHandler_Should_RejectMalformedTokens(string invalidToken)
    {
        // Arrange
        var handler = new JwtSecurityTokenHandler();

        // Act
        var canRead = handler.CanReadToken(invalidToken);

        // Assert
        Assert.False(canRead, $"Handler should reject malformed token: {invalidToken}");
    }

    [Fact]
    public void JwtToken_Should_HandleEmptyScope()
    {
        // Arrange
        var token = CreateTestToken("");
        var handler = new JwtSecurityTokenHandler();

        // Act
        var jwtToken = handler.ReadJwtToken(token);
        var scopeClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "scope");
        var scopes = scopeClaim?.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

        // Assert
        Assert.NotNull(scopeClaim);
        Assert.Empty(scopes);
    }
}
