namespace AmLink.Submissions.Mcp.Client.Configuration;

public sealed class McpClientConfiguration
{
    public const string SectionName = "McpServer";

    public required string Url { get; set; }
    public string? BrowserUrl { get; set; }
    public string? Name { get; set; }
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets the URL that should be used by the browser/client-side code.
    /// Falls back to Url if BrowserUrl is not specified.
    /// </summary>
    public string GetBrowserUrl() => BrowserUrl ?? Url;
}
