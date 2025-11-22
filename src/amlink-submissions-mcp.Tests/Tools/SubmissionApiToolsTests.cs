using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol;
using Moq;
using Moq.Protected;
using Xunit;
using IDSProtectedMcpServer.Tools;
using AmLink.Submission.Mcp.Server.Configuration;

namespace amlink_submissions_mcp.Tests.Tools;

/// <summary>
/// Comprehensive tests for SubmissionApiTools covering both JWT validation and GetSubmission functionality
/// </summary>
public class SubmissionApiToolsTests
{
    #region Test Constants

    private const long ValidSubmissionId = 12345L;
    private const string ValidJsonResponse = "{\"id\": 12345, \"status\": \"active\", \"submitter\": \"test@example.com\"}";
    private const string TestBearerToken = "Bearer test-token";
    private const string TestApiBaseUrl = "https://api.test.com/";
    private const string TestUserAgent = "test-client/1.0";

    #endregion
    #region TDD Tests for GetSubmission Method (from main branch)

    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IOptions<IdentityServerConfiguration>> _mockIdsOptions;
    private readonly Mock<IOptions<ExternalApisConfiguration>> _mockExternalApisOptions;
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly SubmissionApiTools _submissionApiTools;

    public SubmissionApiToolsTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockIdsOptions = new Mock<IOptions<IdentityServerConfiguration>>();
        _mockExternalApisOptions = new Mock<IOptions<ExternalApisConfiguration>>();
        _mockHttpHandler = new Mock<HttpMessageHandler>();

        _httpClient = new HttpClient(_mockHttpHandler.Object)
        {
            BaseAddress = new Uri(TestApiBaseUrl)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(TestUserAgent);

        // Setup configuration mocks
        var idsConfig = new IdentityServerConfiguration
        {
            Url = "https://identity.test.com",
            ClientId = "test-client",
            ClientSecret = "test-secret",
            GrantType = "client_credentials",
            Scopes = "submission-api"
        };

        var externalApisConfig = new ExternalApisConfiguration
        {
            SubmissionApi = new SubmissionApiConfiguration
            {
                BaseUrl = TestApiBaseUrl.TrimEnd('/'),
                RequiredScope = TestScope,
                UserAgent = "test-client",
                Version = "1.0"
            }
        };

        _mockIdsOptions.Setup(x => x.Value).Returns(idsConfig);
        _mockExternalApisOptions.Setup(x => x.Value).Returns(externalApisConfig);
        _mockHttpClientFactory.Setup(x => x.CreateClient("SubmissionApi")).Returns(_httpClient);

        // Setup HttpContext with a valid Bearer token
        var mockHttpContext = new Mock<HttpContext>();
        var mockRequest = new Mock<HttpRequest>();
        var mockHeaders = new Mock<IHeaderDictionary>();

        // Mock the Authorization header properly for ASP.NET Core
        var authHeaderValue = new Microsoft.Extensions.Primitives.StringValues(TestBearerToken);
        mockHeaders.Setup(h => h["Authorization"]).Returns(authHeaderValue);
        mockHeaders.Setup(h => h.TryGetValue("Authorization", out It.Ref<Microsoft.Extensions.Primitives.StringValues>.IsAny))
            .Returns((string key, out Microsoft.Extensions.Primitives.StringValues values) =>
            {
                values = authHeaderValue;
                return true;
            });
        mockRequest.Setup(r => r.Headers).Returns(mockHeaders.Object);
        mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        // Create the tools instance
        var mockLogger = new Mock<ILogger<SubmissionApiTools>>();
        _submissionApiTools = new SubmissionApiTools(
            _mockHttpClientFactory.Object,
            _mockHttpContextAccessor.Object,
            _mockConfiguration.Object,
            mockLogger.Object,
            _mockIdsOptions.Object,
            _mockExternalApisOptions.Object
        );
    }

    [Fact]
    public async Task GetSubmission_WithZeroSubmissionId_ShouldThrowArgumentException()
    {
        // Arrange - zero submission ID
        long submissionId = 0;

        // Act & Assert - Should validate parameters BEFORE making HTTP calls
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _submissionApiTools.GetSubmission(submissionId));

        Assert.Equal("submissionId", exception.ParamName);
        Assert.Contains("must be a positive integer", exception.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(long.MinValue)]
    public async Task GetSubmission_WithNegativeSubmissionId_ShouldThrowArgumentException(long submissionId)
    {
        // Act & Assert - Should validate negative parameters
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _submissionApiTools.GetSubmission(submissionId));

        Assert.Equal("submissionId", exception.ParamName);
        Assert.Contains("must be a positive integer", exception.Message);
    }

    [Fact]
    public async Task GetSubmission_WithValidId_ShouldReturnCleanJsonObject()
    {
        // Arrange
        var submissionId = ValidSubmissionId;
        var expectedJsonResponse = ValidJsonResponse;

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(expectedJsonResponse, Encoding.UTF8, "application/json")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _submissionApiTools.GetSubmission(submissionId);

        // Assert - Should return clean JSON, not wrapped in "Submission Details:" text
        var parsedResult = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.Equal(12345, parsedResult.GetProperty("id").GetInt64());
        Assert.Equal("active", parsedResult.GetProperty("status").GetString());
        Assert.Equal("test@example.com", parsedResult.GetProperty("submitter").GetString());
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task GetSubmission_WhenApiReturnsError_ShouldThrowSpecificHttpException(HttpStatusCode statusCode)
    {
        // Arrange
        var submissionId = ValidSubmissionId;
        var httpResponse = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent("Error occurred", Encoding.UTF8, "text/plain")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act & Assert - Should throw HTTP-specific exceptions, not generic McpException
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => _submissionApiTools.GetSubmission(submissionId));

        Assert.Contains(statusCode.ToString(), exception.Message);
    }

    [Fact]
    public async Task GetSubmission_WithValidId_ShouldMakeCorrectHttpRequest()
    {
        // Arrange
        var submissionId = ValidSubmissionId;
        var expectedJsonResponse = "{\"id\": 12345}";
        HttpRequestMessage? capturedRequest = null;

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(expectedJsonResponse, Encoding.UTF8, "application/json")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse)
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request);

        // Act
        await _submissionApiTools.GetSubmission(submissionId);

        // Assert - Should make proper HTTP request with correct headers and URL
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest.Method);
        Assert.Contains($"submissions/{submissionId}", capturedRequest.RequestUri?.ToString());
        Assert.Contains("Bearer", capturedRequest.Headers.Authorization?.ToString());
        Assert.Equal("test-client", capturedRequest.Headers.UserAgent.First().Product?.Name);
    }

    [Fact]
    public async Task GetSubmission_WhenApiReturnsInvalidJson_ShouldThrowJsonException()
    {
        // Arrange
        var submissionId = ValidSubmissionId;
        var invalidJsonResponse = "{ invalid json }";

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(invalidJsonResponse, Encoding.UTF8, "application/json")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act & Assert - Should handle JSON parsing errors gracefully
        await Assert.ThrowsAsync<JsonException>(
            () => _submissionApiTools.GetSubmission(submissionId));
    }

    [Fact]
    public async Task GetSubmission_WhenNetworkTimeout_ThrowsTaskCanceledException()
    {
        // Arrange
        var submissionId = ValidSubmissionId;

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timed out"));

        // Act & Assert - Should handle timeouts appropriately
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _submissionApiTools.GetSubmission(submissionId));
    }

    [Fact]
    public async Task GetSubmission_WithMaxLongValue_ShouldHandleLargeIds()
    {
        // Arrange
        var submissionId = long.MaxValue; // 9223372036854775807
        var expectedJsonResponse = $"{{\"id\": {submissionId}, \"status\": \"active\"}}";

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(expectedJsonResponse, Encoding.UTF8, "application/json")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _submissionApiTools.GetSubmission(submissionId);

        // Assert - Should handle very large ID values
        var parsedResult = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.Equal(submissionId, parsedResult.GetProperty("id").GetInt64());
    }

    [Fact]
    public async Task GetSubmission_WhenApiReturnsEmptyResponse_ShouldThrowJsonException()
    {
        // Arrange
        var submissionId = ValidSubmissionId;
        var emptyResponse = "";

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(emptyResponse, Encoding.UTF8, "application/json")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act & Assert - Should throw JsonException for empty response
        await Assert.ThrowsAsync<JsonException>(
            () => _submissionApiTools.GetSubmission(submissionId));
    }

    [Fact]
    public async Task GetSubmission_WhenApiReturnsNonJsonContentType_ShouldStillProcessResponse()
    {
        // Arrange
        var submissionId = ValidSubmissionId;
        var validJsonResponse = "{\"id\": 12345, \"status\": \"active\"}";

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(validJsonResponse, Encoding.UTF8, "text/plain") // Wrong content type
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _submissionApiTools.GetSubmission(submissionId);

        // Assert - Should process valid JSON regardless of content type
        var parsedResult = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.Equal(12345, parsedResult.GetProperty("id").GetInt64());
        Assert.Equal("active", parsedResult.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetSubmission_WhenHttpContextMissing_ShouldThrowMcpException()
    {
        // Arrange
        var submissionId = ValidSubmissionId;

        // Setup HttpContextAccessor to return null (simulating missing context)
        var mockHttpContextAccessorNoContext = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessorNoContext.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var mockLogger = new Mock<ILogger<SubmissionApiTools>>();
        var toolsWithNoContext = new SubmissionApiTools(
            _mockHttpClientFactory.Object,
            mockHttpContextAccessorNoContext.Object,
            _mockConfiguration.Object,
            mockLogger.Object,
            _mockIdsOptions.Object,
            _mockExternalApisOptions.Object
        );

        // Act & Assert - Should throw McpException when HttpContext is missing
        var exception = await Assert.ThrowsAsync<McpException>(
            () => toolsWithNoContext.GetSubmission(submissionId));

        Assert.Contains("HTTP context not available", exception.Message);
    }

    [Theory]
    [InlineData("")] // Empty token
    [InlineData("InvalidToken")] // No Bearer prefix
    [InlineData("Basic dGVzdDp0ZXN0")] // Wrong auth type
    [InlineData("Bearer")] // Bearer with no token
    [InlineData("Bearer ")] // Bearer with space but no token
    public async Task GetSubmission_WithInvalidAuthorizationHeader_ShouldThrowMcpException(string authHeader)
    {
        // Arrange
        var submissionId = ValidSubmissionId;

        // Setup HttpContext with invalid Authorization header
        var mockHttpContextAccessorInvalid = CreateMockHttpContextAccessor(authHeader);

        var mockLogger = new Mock<ILogger<SubmissionApiTools>>();
        var toolsWithInvalidAuth = new SubmissionApiTools(
            _mockHttpClientFactory.Object,
            mockHttpContextAccessorInvalid.Object,
            _mockConfiguration.Object,
            mockLogger.Object,
            _mockIdsOptions.Object,
            _mockExternalApisOptions.Object
        );

        // Act & Assert - Should throw McpException for invalid auth header
        // The exception should be thrown from GetSubmissionApiTokenAsync before any HTTP call
        var exception = await Assert.ThrowsAsync<McpException>(
            () => toolsWithInvalidAuth.GetSubmission(submissionId));

        Assert.Contains("No valid bearer token found in request", exception.Message);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a mock HttpContext with the specified authorization header
    /// </summary>
    private static Mock<IHttpContextAccessor> CreateMockHttpContextAccessor(string? authHeaderValue = null)
    {
        var mockHttpContext = new Mock<HttpContext>();
        var mockRequest = new Mock<HttpRequest>();
        var mockHeaders = new Mock<IHeaderDictionary>();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        if (!string.IsNullOrEmpty(authHeaderValue))
        {
            var headerValue = new Microsoft.Extensions.Primitives.StringValues(authHeaderValue);
            mockHeaders.Setup(h => h["Authorization"]).Returns(headerValue);
            mockHeaders.Setup(h => h.TryGetValue("Authorization", out It.Ref<Microsoft.Extensions.Primitives.StringValues>.IsAny))
                .Returns((string key, out Microsoft.Extensions.Primitives.StringValues values) =>
                {
                    values = headerValue;
                    return true;
                });
        }
        else
        {
            mockHeaders.Setup(h => h.TryGetValue("Authorization", out It.Ref<Microsoft.Extensions.Primitives.StringValues>.IsAny))
                .Returns((string key, out Microsoft.Extensions.Primitives.StringValues values) =>
                {
                    values = default;
                    return false;
                });
        }

        mockRequest.Setup(r => r.Headers).Returns(mockHeaders.Object);
        mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        return mockHttpContextAccessor;
    }

    #endregion

    #region JWT Token Validation Tests (from PR #13 branch)

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
            // No scope claim
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
    public void TokenHasRequiredScope_WithValidTokenAndMatchingScope_ReturnsTrue()
    {
        // Arrange
        var token = CreateTestToken(TestScope);

        // Act
        var result = CallTokenHasRequiredScope(token, TestScope);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TokenHasRequiredScope_WithValidTokenAndMultipleScopes_ReturnsTrue()
    {
        // Arrange
        var token = CreateTestToken($"{TestScope} {OtherScope} read:user");

        // Act
        var result = CallTokenHasRequiredScope(token, TestScope);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TokenHasRequiredScope_WithValidTokenButWrongScope_ReturnsFalse()
    {
        // Arrange
        var token = CreateTestToken(OtherScope);

        // Act
        var result = CallTokenHasRequiredScope(token, TestScope);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TokenHasRequiredScope_WithExpiredToken_ReturnsFalse()
    {
        // Arrange
        var expiredTime = DateTime.UtcNow.AddHours(-1);
        var token = CreateTestToken(TestScope, expiredTime);

        // Act
        var result = CallTokenHasRequiredScope(token, TestScope);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TokenHasRequiredScope_WithInvalidTokenFormat_ReturnsFalse()
    {
        // Arrange
        var invalidToken = CreateInvalidToken();

        // Act
        var result = CallTokenHasRequiredScope(invalidToken, TestScope);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TokenHasRequiredScope_WithTokenWithoutScopeClaim_ReturnsFalse()
    {
        // Arrange
        var token = CreateTokenWithoutScope();

        // Act
        var result = CallTokenHasRequiredScope(token, TestScope);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TokenHasRequiredScope_WithEmptyScope_ReturnsFalse()
    {
        // Arrange
        var token = CreateTestToken("");

        // Act
        var result = CallTokenHasRequiredScope(token, TestScope);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TokenHasRequiredScope_WithNullToken_ReturnsFalse()
    {
        // Act
        var result = CallTokenHasRequiredScope(null!, TestScope);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TokenHasRequiredScope_WithEmptyToken_ReturnsFalse()
    {
        // Act
        var result = CallTokenHasRequiredScope("", TestScope);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TokenHasRequiredScope_WithWhitespaceToken_ReturnsFalse()
    {
        // Act
        var result = CallTokenHasRequiredScope("   ", TestScope);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void TokenHasRequiredScope_WithInvalidRequiredScope_ReturnsFalse(string? requiredScope)
    {
        // Arrange
        var token = CreateTestToken(TestScope);

        // Act
        var result = CallTokenHasRequiredScope(token, requiredScope!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TokenHasRequiredScope_WithScopeContainingSpecialCharacters_ReturnsTrue()
    {
        // Arrange
        var specialScope = "api:read+write";
        var token = CreateTestToken($"{specialScope} other-scope");

        // Act
        var result = CallTokenHasRequiredScope(token, specialScope);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TokenHasRequiredScope_WithCaseSensitiveScope_ReturnsFalse()
    {
        // Arrange
        var token = CreateTestToken("Submission-API");

        // Act
        var result = CallTokenHasRequiredScope(token, "submission-api");

        // Assert
        Assert.False(result); // Should be case-sensitive
    }

    [Fact]
    public void TokenHasRequiredScope_WithPartialScopeMatch_ReturnsFalse()
    {
        // Arrange
        var token = CreateTestToken("submission-api-read");

        // Act
        var result = CallTokenHasRequiredScope(token, "submission-api");

        // Assert
        Assert.False(result); // Should not match partial scopes
    }

    [Fact]
    public void TokenHasRequiredScope_WithTokenJustAboutToExpire_ReturnsTrue()
    {
        // Arrange - Token expires in 30 seconds
        var almostExpiredTime = DateTime.UtcNow.AddSeconds(30);
        var token = CreateTestToken(TestScope, almostExpiredTime);

        // Act
        var result = CallTokenHasRequiredScope(token, TestScope);

        // Assert
        Assert.True(result); // Should still be valid
    }

    /// <summary>
    /// Helper method to call the private TokenHasRequiredScope method via reflection.
    /// </summary>
    private bool CallTokenHasRequiredScope(string token, string requiredScope)
    {
        var mockLogger = new Mock<ILogger<SubmissionApiTools>>();
        var tools = new SubmissionApiTools(
            _mockHttpClientFactory.Object,
            _mockHttpContextAccessor.Object,
            _mockConfiguration.Object,
            mockLogger.Object,
            _mockIdsOptions.Object,
            _mockExternalApisOptions.Object
        );

        var method = typeof(SubmissionApiTools).GetMethod("TokenHasRequiredScope",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method == null)
        {
            throw new InvalidOperationException("TokenHasRequiredScope method not found");
        }

        var result = method.Invoke(tools, new object[] { token, requiredScope });
        return (bool)result!;
    }

    #endregion
}
