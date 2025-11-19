using AmLink.Submissions.Mcp.Client.Configuration;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Net.Http.Headers;

namespace AmLink.Submissions.Mcp.Client.Services;

public interface IMcpService
{
    Task<McpClient> CreateAuthenticatedClientAsync(CancellationToken cancellationToken = default);
    Task<List<McpClientTool>> GetAvailableToolsAsync(CancellationToken cancellationToken = default);
    Task<string> InvokeToolAsync(string toolName, IReadOnlyDictionary<string, object?>? parameters, CancellationToken cancellationToken = default);
}

public class McpService : IMcpService
{
    private readonly McpClientConfiguration _mcpConfig;
    private readonly ITokenService _tokenService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<McpService> _logger;

    public McpService(
        McpClientConfiguration mcpConfig,
        ITokenService tokenService,
        IHttpClientFactory httpClientFactory,
        ILogger<McpService> logger)
    {
        _mcpConfig = mcpConfig;
        _tokenService = tokenService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<McpClient> CreateAuthenticatedClientAsync(CancellationToken cancellationToken = default)
    {
        var accessToken = await _tokenService.GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(accessToken))
        {
            _logger.LogWarning("No valid access token available for MCP client creation");
            throw new UnauthorizedAccessException("No valid access token available. Please authenticate first.");
        }

        var httpClient = _httpClientFactory.CreateClient("mcp-client");
        
        // Add the bearer token to the HTTP client
        httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", accessToken);

        var consoleLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        
        _logger.LogInformation("Creating authenticated MCP client for endpoint: {Endpoint}", _mcpConfig.Url);
        
        // Create MCP transport WITHOUT OAuth (since we're pre-authenticated)
        var transport = new HttpClientTransport(new()
        {
            Endpoint = new Uri(_mcpConfig.Url),
            Name = "Pre-Authenticated MCP Client"
        }, httpClient, consoleLoggerFactory);

        return await McpClient.CreateAsync(transport);
    }

    public async Task<List<McpClientTool>> GetAvailableToolsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await CreateAuthenticatedClientAsync(cancellationToken);
            _logger.LogInformation("Connecting to MCP server to retrieve available tools");
            
            var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
            _logger.LogInformation("Successfully retrieved {ToolCount} tools from MCP server", tools.Count());
            
            return tools.ToList();
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Unauthorized access when retrieving tools - token may have expired");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve available tools from MCP server");
            throw new InvalidOperationException($"Failed to retrieve tools: {ex.Message}", ex);
        }
    }

    public async Task<string> InvokeToolAsync(string toolName, IReadOnlyDictionary<string, object?>? arguments, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await CreateAuthenticatedClientAsync(cancellationToken);
            _logger.LogInformation("Invoking tool: {ToolName}", toolName);
            
            var result = await client.CallToolAsync(toolName, arguments, cancellationToken: cancellationToken);

            var content = result.Content?.FirstOrDefault() as TextContentBlock;
            var resultText = content?.Text ?? "No result returned";
            _logger.LogInformation("Tool {ToolName} executed successfully", toolName);
            
            return resultText;
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Unauthorized access when invoking tool {ToolName} - token may have expired", toolName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invoke tool: {ToolName}", toolName);
            throw new InvalidOperationException($"Failed to invoke tool '{toolName}': {ex.Message}", ex);
        }
    }
}