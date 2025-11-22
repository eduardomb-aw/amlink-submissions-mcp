using System.Text;
using System.Text.Json;
using AmLink.Submissions.Mcp.Client.Configuration;
using AmLink.Submissions.Mcp.Client.Model;

namespace AmLink.Submissions.Mcp.Client.Services;

public interface IOpenAiService
{
    Task<string> SendChatMessageAsync(string message, CancellationToken cancellationToken = default);
}

public class OpenAiService : IOpenAiService
{
    private readonly HttpClient _httpClient;
    private readonly IMcpService _mcpService;
    private readonly ILogger<OpenAiService> _logger;
    private readonly string _apiKey;

    public OpenAiService(
        IHttpClientFactory httpClientFactory,
        IMcpService mcpService,
        IConfiguration configuration,
        ILogger<OpenAiService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("openai");
        _mcpService = mcpService;
        _logger = logger;

        _apiKey = configuration["OPENAI_API_KEY"] ??
                 throw new InvalidOperationException("OPENAI_API_KEY is required but not configured");

        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        _httpClient.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");
    }

    public async Task<string> SendChatMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending chat message to OpenAI with MCP tools integration");

            // Get available MCP tools
            var mcpTools = await GetMcpToolsAsync(cancellationToken);

            // Prepare the chat completion request
            var request = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful AI assistant that can use various tools to help users. When appropriate, use the available tools to provide more accurate and helpful responses." },
                    new { role = "user", content = message }
                },
                tools = mcpTools,
                tool_choice = "auto",
                temperature = 0.7,
                max_tokens = 4000
            };

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("OpenAI API request failed: {StatusCode} {Content}", response.StatusCode, errorContent);
                throw new InvalidOperationException($"OpenAI API error: {response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(responseJson);

            var choices = document.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() == 0)
            {
                return "I'm sorry, I couldn't generate a response.";
            }

            var firstChoice = choices[0];
            var responseMessage = firstChoice.GetProperty("message");

            // Check if there are tool calls
            if (responseMessage.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.GetArrayLength() > 0)
            {
                return await HandleToolCallsAsync(message, responseMessage, cancellationToken);
            }

            // Regular response without tool calls
            if (responseMessage.TryGetProperty("content", out var contentProperty) && contentProperty.ValueKind == JsonValueKind.String)
            {
                return contentProperty.GetString() ?? "No response generated.";
            }

            return "I'm sorry, I couldn't generate a response.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to OpenAI: {Message}", message);
            return $"I'm sorry, I encountered an error: {ex.Message}";
        }
    }

    private async Task<object[]> GetMcpToolsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var mcpTools = await _mcpService.GetAvailableToolsAsync(cancellationToken);

            return mcpTools.Select(tool => new
            {
                type = "function",
                function = new
                {
                    name = tool.Name,
                    description = tool.Description,
                    parameters = GetToolParametersSchema(tool)
                }
            }).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve MCP tools, continuing without them");
            return Array.Empty<object>();
        }
    }

    private object GetToolParametersSchema(ModelContextProtocol.Client.McpClientTool tool)
    {
        // For common submission-related tools, provide explicit schemas
        return tool.Name?.ToLowerInvariant() switch
        {
            "get_submission" => new
            {
                type = "object",
                properties = new Dictionary<string, object>
                {
                    ["submissionId"] = new { type = "string", description = "The submission ID to retrieve details for" }
                },
                required = new[] { "submissionId" }
            },
            "search_submissions" => new
            {
                type = "object",
                properties = new Dictionary<string, object>
                {
                    ["query"] = new { type = "string", description = "Search query for submissions" },
                    ["limit"] = new { type = "integer", description = "Maximum number of results to return" }
                },
                required = new[] { "query" }
            },
            _ => new
            {
                type = "object",
                properties = new Dictionary<string, object>(),
                required = Array.Empty<string>()
            }
        };
    }

    private async Task<string> HandleToolCallsAsync(string originalMessage, JsonElement responseMessage, CancellationToken cancellationToken)
    {
        try
        {
            var toolCalls = responseMessage.GetProperty("tool_calls");
            var toolResults = new List<string>();

            foreach (var toolCall in toolCalls.EnumerateArray())
            {
                var function = toolCall.GetProperty("function");
                var functionName = function.GetProperty("name").GetString();
                var argumentsJson = function.GetProperty("arguments").GetString();

                if (string.IsNullOrEmpty(functionName) || string.IsNullOrEmpty(argumentsJson))
                    continue;

                _logger.LogInformation("Executing MCP tool: {ToolName} with arguments: {Arguments}", functionName, argumentsJson);

                // Parse arguments - handle both simple and nested JSON structures
                Dictionary<string, object?> arguments;
                try
                {
                    using var document = JsonDocument.Parse(argumentsJson);
                    arguments = new Dictionary<string, object?>();

                    foreach (var property in document.RootElement.EnumerateObject())
                    {
                        arguments[property.Name] = property.Value.ValueKind switch
                        {
                            JsonValueKind.String => property.Value.GetString(),
                            JsonValueKind.Number => property.Value.GetDecimal(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.Null => null,
                            _ => property.Value.GetRawText()
                        };
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse tool arguments: {Arguments}", argumentsJson);
                    continue;
                }

                // Call the MCP tool
                var result = await _mcpService.InvokeToolAsync(functionName, arguments, cancellationToken);
                toolResults.Add($"**{functionName}**: {result}");
            }

            if (toolResults.Any())
            {
                // Send a follow-up request to OpenAI with the tool results
                var followUpRequest = new
                {
                    model = "gpt-4o",
                    messages = new[]
                    {
                        new { role = "system", content = "You are a helpful AI assistant. Use the tool results to provide a comprehensive answer to the user's question." },
                        new { role = "user", content = originalMessage },
                        new { role = "assistant", content = $"I've gathered information using available tools:\n\n{string.Join("\n\n", toolResults)}\n\nLet me provide you with a comprehensive answer based on this information." }
                    },
                    temperature = 0.7,
                    max_tokens = 4000
                };

                var followUpJson = JsonSerializer.Serialize(followUpRequest, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                var followUpContent = new StringContent(followUpJson, Encoding.UTF8, "application/json");
                var followUpResponse = await _httpClient.PostAsync("chat/completions", followUpContent, cancellationToken);

                if (followUpResponse.IsSuccessStatusCode)
                {
                    var followUpResponseJson = await followUpResponse.Content.ReadAsStringAsync(cancellationToken);
                    using var followUpDocument = JsonDocument.Parse(followUpResponseJson);

                    var followUpChoices = followUpDocument.RootElement.GetProperty("choices");
                    if (followUpChoices.GetArrayLength() > 0)
                    {
                        var followUpChoice = followUpChoices[0];
                        var followUpMessage = followUpChoice.GetProperty("message");

                        if (followUpMessage.TryGetProperty("content", out var followUpContentProperty) &&
                            followUpContentProperty.ValueKind == JsonValueKind.String)
                        {
                            return followUpContentProperty.GetString() ?? string.Join("\n\n", toolResults);
                        }
                    }
                }

                // Fallback to tool results if follow-up fails
                return string.Join("\n\n", toolResults);
            }

            return "I executed some tools but didn't get any results.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling tool calls");
            return $"I tried to use some tools to help answer your question, but encountered an error: {ex.Message}";
        }
    }
}
