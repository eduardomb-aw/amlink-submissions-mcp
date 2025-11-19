namespace AmLink.Submission.Mcp.Server.Configuration;

public sealed class IdentityServerConfiguration
{
    public const string SectionName = "IdentityServer";
    
    public required string Url { get; set; }
    public required string ClientId { get; set; }
    public string ClientSecret { get; set; } = string.Empty;
    public required string GrantType { get; set; }
    public required string Scopes { get; set; }
    
    public string TokenEndpoint => $"{Url.TrimEnd('/')}/connect/token";
    public List<string> ScopesList => Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
}