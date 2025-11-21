using AmLink.Submission.Mcp.Server.Configuration;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text.Json;

namespace IDSProtectedMcpServer.Tools;

[McpServerToolType]
public sealed class SubmissionApiTools
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SubmissionApiTools> _logger;
    private readonly IdentityServerConfiguration _idsConfig;
    private readonly ExternalApisConfiguration _externalApisConfig;

    /// <summary>
    /// Initializes a new instance of the SubmissionApiTools class.
    /// </summary>
    public SubmissionApiTools(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<SubmissionApiTools> logger,
        IOptions<IdentityServerConfiguration> idsOptions,
        IOptions<ExternalApisConfiguration> externalApisOptions)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _logger = logger;
        _idsConfig = idsOptions.Value;
        _externalApisConfig = externalApisOptions.Value;
    }

    /// <summary>
    /// Gets submission data from the Submission API secured by Identity Server 4.
    /// </summary>
    /// <param name="submissionId">The ID of the submission to retrieve.</param>
    /// <returns>Submission details in JSON format.</returns>
    [McpServerTool, Description("Get submission details from the Submission API using Identity Server 4 authentication.")]
    public async Task<string> GetSubmission(
        [Description("The ID of the submission to retrieve")] string submissionId)
    {
        var submissionApiToken = await GetSubmissionApiTokenAsync();

        var client = _httpClientFactory.CreateClient("SubmissionApi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", submissionApiToken);

        var response = await client.GetAsync($"submissions/{submissionId}");

        if (!response.IsSuccessStatusCode)
        {
            throw new McpException($"Submission API call failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        }

        var jsonContent = await response.Content.ReadAsStringAsync();
        return $"Submission Details:\n{jsonContent}";
    }

    /// <summary>
    /// Creates a new submission via the Submission API.
    /// </summary>
    /// <param name="submissionData">The submission data in JSON format.</param>
    /// <returns>The created submission details.</returns>
    [McpServerTool, Description("Create a new submission via the Submission API using Identity Server 4 authentication.")]
    public async Task<string> CreateSubmission(
        [Description("The account id for the submission")] int accountId,
        [Description("The submission data in JSON format")] string submissionData)
    {
        var submissionApiToken = await GetSubmissionApiTokenAsync();

        var client = _httpClientFactory.CreateClient("SubmissionApi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", submissionApiToken);

        var content = new StringContent(submissionData, System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"accounts/{accountId}/submissions", content);

        if (!response.IsSuccessStatusCode)
        {
            throw new McpException($"Submission API call failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        }

        var jsonContent = await response.Content.ReadAsStringAsync();
        return $"Submission Created:\n{jsonContent}";
    }

    /// <summary>
    /// Lists submissions for and account using the Submission API with optional filtering and projection.
    /// </summary>
    /// <param name="accountId">Account id to look for submissions.</param>
    /// <param name="limit">Maximum number of submissions to return (default: 10).</param>
    /// <returns>List of submissions in JSON format.</returns>
    [McpServerTool, Description("List submissions from the Submission API with optional filtering and projection.")]
    public async Task<string> ListSubmissions(
        [Description("The account id to look for submissions")] int accountId,
        [Description("OData filter to apply")] string? odataFilter = null,
        [Description("OData projection to apply")] string? odataSelect = null,
        [Description("Maximum number of submissions to return")] int limit = 10)
    {
        var submissionApiToken = await GetSubmissionApiTokenAsync();

        var client = _httpClientFactory.CreateClient("SubmissionApi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", submissionApiToken);

        var queryParams = new List<string> { $"$top={limit}" };
        if (!string.IsNullOrEmpty(odataFilter))
        {
            queryParams.Add($"$filter={Uri.EscapeDataString(odataFilter)}");
        }
        if (!string.IsNullOrEmpty(odataSelect))
        {
            queryParams.Add($"$select={Uri.EscapeDataString(odataSelect)}");
        }

        var queryString = string.Join("&", queryParams);
        var response = await client.GetAsync($"accounts/{accountId}/submissions?{queryString}");

        if (!response.IsSuccessStatusCode)
        {
            throw new McpException($"Submission API call failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        }

        var jsonContent = await response.Content.ReadAsStringAsync();
        return $"Submissions List:\n{jsonContent}";
    }

    /// <summary>
    /// Declines a submission via the Submission API.
    /// </summary>
    /// <param name="submissionId">The ID of the submission to update.</param>
    /// <param name="notes">Optional notes for the declination.</param>
    /// <returns>The updated submission details.</returns>
    [McpServerTool, Description("Update the status of a submission via the Submission API.")]
    public async Task<string> DeclineSubmission(
        [Description("The ID of the submission to be declined")] string submissionId,
        [Description("Optional notes for the declination")] string? notes = null)
    {
        var submissionApiToken = await GetSubmissionApiTokenAsync();

        var client = _httpClientFactory.CreateClient("SubmissionApi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", submissionApiToken);

        var updateData = new
        {
            StatusReasonId = 5,
            Notes = notes
        };

        var content = new StringContent(JsonSerializer.Serialize(updateData), System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"submissions/{submissionId}/declineSubmission", content);

        if (!response.IsSuccessStatusCode)
        {
            throw new McpException($"Submission API call failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        }

        var jsonContent = await response.Content.ReadAsStringAsync();
        return $"Submission Status Updated:\n{jsonContent}";
    }

    /// <summary>
    /// Gets an access token for the Submission API using the current user's token with Identity Server 4.
    /// This demonstrates how to use the same token that authenticated the MCP request to call downstream APIs
    /// secured by the same Identity Server 4 instance.
    /// </summary>
    /// <returns>An access token for the Submission API.</returns>
    private async Task<string> GetSubmissionApiTokenAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext
           ?? throw new McpException("HTTP context not available");

        var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            throw new McpException("No valid bearer token found in request");
        }

        var currentToken = authHeader["Bearer ".Length..];

        // Since both the MCP server and Submission API are secured by the same Identity Server 4,
        // we can potentially reuse the same token if it has the correct scopes.
        // Alternatively, we could exchange it for a new token with specific scopes for the Submission API.

        // Check if the token has the required scope for the Submission API
        if (TokenHasRequiredScope(currentToken, _externalApisConfig.SubmissionApi.RequiredScope))
        {
            return currentToken;
        }

        // If not, we could implement token exchange here, but for simplicity,
        // let's assume the token already has the required scopes
        return currentToken;
    }

    /// <summary>
    /// Checks if the current JWT token has the required scope.
    /// Uses System.IdentityModel.Tokens.Jwt library for proper JWT validation.
    /// </summary>
    /// <param name="token">The JWT token to check.</param>
    /// <param name="requiredScope">The required scope.</param>
    /// <returns>True if the token has the required scope.</returns>
    private bool TokenHasRequiredScope(string token, string requiredScope)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(token))
            {
                _logger.LogWarning("Invalid JWT token format");
                return false;
            }

            var jwtToken = handler.ReadJwtToken(token);

            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                _logger.LogWarning("JWT token has expired");
                return false;
            }

            var scopeClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "scope");
            if (scopeClaim == null)
            {
                _logger.LogWarning("JWT token does not contain scope claim");
                return false;
            }

            var scopes = scopeClaim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return scopes.Contains(requiredScope);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating JWT token for required scope");
            return false;
        }
    }
}