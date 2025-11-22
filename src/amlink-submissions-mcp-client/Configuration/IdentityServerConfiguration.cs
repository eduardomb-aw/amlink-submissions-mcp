namespace AmLink.Submissions.Mcp.Client.Configuration;

public sealed class IdentityServerConfiguration
{
    public const string SectionName = "IdentityServer";

    public required string Url { get; set; }
    public required string ClientId { get; set; }
    public string ClientSecret { get; set; } = string.Empty;
    public required string GrantType { get; set; }
    public required string Scopes { get; set; }
    public required string ServerClientId { get; set; }
    public required string RedirectUri { get; set; }
    public string ResponseMode { get; set; } = "query";

    // Derived properties for OAuth endpoints
    public string AuthorityUrl => Url;
    public string TokenEndpoint => $"{Url.TrimEnd('/')}/connect/token";
    public string AuthorizationEndpoint => $"{Url.TrimEnd('/')}/connect/authorize";
    public List<string> ScopesList => Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
}
