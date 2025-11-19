using AmLink.Submissions.Mcp.Client.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ModelContextProtocol.Client;
using static AmLink.Submissions.Mcp.Client.Model.IndexModel;

namespace AmLink.Submissions.Mcp.Client.Pages;

public partial class IndexModel : PageModel
{
    private readonly ITokenService _tokenService;
    private readonly IMcpService _mcpService;
    private readonly ILogger<IndexModel> _logger;
    
    public string? ErrorMessage { get; set; }
    public string? AuthUrl { get; set; }
    public string? SuccessMessage { get; set; }
    public bool IsAuthenticated => _tokenService.IsAuthenticated;
    public bool IsConnected { get; set; }
    public List<McpClientTool>? AvailableTools { get; set; }
    public string? ToolResult { get; set; }
    
    public IndexModel(
        ITokenService tokenService,
        IMcpService mcpService,
        ILogger<IndexModel> logger)
    {
        _tokenService = tokenService;
        _mcpService = mcpService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        _logger.LogInformation("Index page loaded. Authentication status: {IsAuthenticated}", IsAuthenticated);
        
        // If already authenticated, try to get tools automatically
        if (IsAuthenticated)
        {
            try
            {
                AvailableTools = await _mcpService.GetAvailableToolsAsync();
                IsConnected = true;
                SuccessMessage = $"Connected to MCP server. Found {AvailableTools.Count} available tools.";
                _logger.LogInformation("Successfully connected to MCP server with {ToolCount} tools", AvailableTools.Count);
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Token expired during automatic connection attempt");
                _tokenService.ClearToken();
                ErrorMessage = "Authentication expired. Please authenticate again.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to auto-connect to MCP server on page load");
                // Don't set error here, let user try manually
            }
        }
    }

    public async Task<IActionResult> OnPostAuthenticateAsync()
    {
        try
        {
            _logger.LogInformation("Initiating authentication process");
            AuthUrl = await _tokenService.InitiateAuthenticationAsync();
            SuccessMessage = "Authentication URL generated. Click 'Authenticate Now' to continue.";
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication initiation failed");
            ErrorMessage = $"Failed to start authentication: {ex.Message}";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostConnectAsync()
    {
        try
        {
            if (!IsAuthenticated)
            {
                _logger.LogWarning("Connection attempt without valid authentication");
                ErrorMessage = "Please authenticate first before connecting to MCP server.";
                return Page();
            }

            _logger.LogInformation("Attempting to connect to MCP server");
            AvailableTools = await _mcpService.GetAvailableToolsAsync();
            IsConnected = true;
            SuccessMessage = $"Successfully connected to MCP server! Found {AvailableTools.Count} available tools.";
            
            return Page();
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Token expired during connection attempt");
            _tokenService.ClearToken();
            ErrorMessage = "Authentication expired. Please authenticate again.";
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to MCP server");
            ErrorMessage = $"Failed to connect: {ex.Message}";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostInvokeToolAsync(string toolName, string parameters = "{}")
    {
        try
        {
            if (!IsAuthenticated)
            {
                ErrorMessage = "Please authenticate first.";
                return Page();
            }

            if (string.IsNullOrEmpty(toolName))
            {
                ErrorMessage = "Tool name is required.";
                return Page();
            }

            _logger.LogInformation("Invoking tool: {ToolName} with parameters: {Parameters}", toolName, parameters);
            
            var parametersObj = System.Text.Json.JsonSerializer.Deserialize<IReadOnlyDictionary<string, object?>>(parameters);
            ToolResult = await _mcpService.InvokeToolAsync(toolName, parametersObj);
            
            SuccessMessage = $"Tool '{toolName}' executed successfully.";
            
            // Refresh the page to show current state
            await OnGetAsync();
            
            return Page();
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Token expired during tool invocation: {ToolName}", toolName);
            _tokenService.ClearToken();
            ErrorMessage = "Authentication expired. Please authenticate again.";
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tool invocation failed for {ToolName}", toolName);
            ErrorMessage = $"Tool invocation failed: {ex.Message}";
            return Page();
        }
    }

    public IActionResult OnPostClearTokenAsync()
    {
        _logger.LogInformation("Clearing authentication token");
        _tokenService.ClearToken();
        IsConnected = false;
        AvailableTools = null;
        ToolResult = null;
        AuthUrl = null;
        SuccessMessage = "Authentication cleared successfully.";
        return Page();
    }
}
