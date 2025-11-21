using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;
using IDSProtectedMcpServer.Tools;
using AmLink.Submission.Mcp.Server.Configuration;

namespace amlink_submissions_mcp.Tests.Tools;

/// <summary>
/// TDD tests for SubmissionApiTools - these describe IDEAL behavior first
/// </summary>
public class SubmissionApiToolsTests
{
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
            BaseAddress = new Uri("https://api.test.com/")
        };

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
                BaseUrl = "https://api.test.com",
                RequiredScope = "submission-api",
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
        var mockHeaders = new HeaderDictionary();

        // Create a simple JWT-like token with the required scope
        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"typ\":\"JWT\",\"alg\":\"HS256\"}"));
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"scope\":\"submission-api\",\"sub\":\"test-user\"}"));
        var signature = Convert.ToBase64String(Encoding.UTF8.GetBytes("test-signature"));
        var testJwtToken = $"{header}.{payload}.{signature}";

        mockHeaders["Authorization"] = $"Bearer {testJwtToken}";

        mockRequest.Setup(r => r.Headers).Returns(mockHeaders);
        mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        _submissionApiTools = new SubmissionApiTools(
            _mockHttpClientFactory.Object,
            _mockHttpContextAccessor.Object,
            _mockConfiguration.Object,
            _mockIdsOptions.Object,
            _mockExternalApisOptions.Object);
    }

    #region RED PHASE - Tests that describe IDEAL behavior (will fail against current implementation)

    [Fact(Skip = "TDD RED phase - implementation pending")]
    public async Task GetSubmission_WithNullSubmissionId_ShouldThrowArgumentNullException()
    {
        // Arrange - null submission ID
        string? submissionId = null;

        // Act & Assert - Should validate parameters BEFORE making HTTP calls
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => _submissionApiTools.GetSubmission(submissionId!));

        Assert.Equal("submissionId", exception.ParamName);
    }

    [Theory(Skip = "TDD RED phase - implementation pending")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public async Task GetSubmission_WithWhitespaceSubmissionId_ShouldThrowArgumentException(string submissionId)
    {
        // Arrange - empty or whitespace submission ID

        // Act & Assert - Should validate parameters BEFORE making HTTP calls
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _submissionApiTools.GetSubmission(submissionId));

        Assert.Equal("submissionId", exception.ParamName);
        Assert.Contains("cannot be empty or whitespace", exception.Message);
    }

    [Fact(Skip = "TDD RED phase - implementation pending")]
    public async Task GetSubmission_WithValidId_ShouldReturnCleanJsonObject()
    {
        // Arrange
        var submissionId = "SUB-12345";
        var expectedSubmission = new
        {
            id = submissionId,
            status = "Active",
            createdDate = "2024-01-15T10:30:00Z",
            submitterName = "John Doe"
        };
        var jsonResponse = JsonSerializer.Serialize(expectedSubmission);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
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
        Assert.Equal(submissionId, parsedResult.GetProperty("id").GetString());
        Assert.Equal("Active", parsedResult.GetProperty("status").GetString());
        Assert.Equal("John Doe", parsedResult.GetProperty("submitterName").GetString());
    }

    [Theory(Skip = "TDD RED phase - implementation pending")]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task GetSubmission_WhenApiReturnsError_ShouldThrowSpecificHttpException(HttpStatusCode statusCode)
    {
        // Arrange
        var submissionId = "SUB-12345";
        var errorMessage = $"API returned {statusCode}";
        var httpResponse = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(errorMessage, System.Text.Encoding.UTF8, "text/plain")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act & Assert - Should throw HttpRequestException with meaningful message
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => _submissionApiTools.GetSubmission(submissionId));

        Assert.Contains(statusCode.ToString(), exception.Message);
        Assert.Contains(errorMessage, exception.Message);
    }

    [Fact(Skip = "TDD RED phase - implementation pending")]
    public async Task GetSubmission_WithValidId_ShouldMakeCorrectHttpRequest()
    {
        // Arrange
        var submissionId = "SUB-12345";
        var jsonResponse = JsonSerializer.Serialize(new { id = submissionId });
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
        };

        HttpRequestMessage? capturedRequest = null;
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(httpResponse);

        // Act
        await _submissionApiTools.GetSubmission(submissionId);

        // Assert - Should make proper HTTP request
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest.Method);
        Assert.Contains($"submissions/{submissionId}", capturedRequest.RequestUri?.ToString());
        Assert.Equal("Bearer", capturedRequest.Headers.Authorization?.Scheme);
        Assert.NotNull(capturedRequest.Headers.Authorization?.Parameter);
    }

    [Fact(Skip = "TDD RED phase - implementation pending")]
    public async Task GetSubmission_WhenApiReturnsInvalidJson_ShouldThrowJsonException()
    {
        // Arrange
        var submissionId = "SUB-12345";
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("invalid json content", System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act & Assert - Should validate JSON and throw JsonException for invalid content
        await Assert.ThrowsAsync<JsonException>(
            () => _submissionApiTools.GetSubmission(submissionId));
    }

    [Fact(Skip = "TDD RED phase - implementation pending")]
    public async Task GetSubmission_WhenNetworkTimeout_ThrowsTaskCanceledException()
    {
        // Arrange
        var submissionId = "SUB-12345";

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _submissionApiTools.GetSubmission(submissionId));
    }

    #endregion

    private void Dispose()
    {
        _httpClient?.Dispose();
    }
}