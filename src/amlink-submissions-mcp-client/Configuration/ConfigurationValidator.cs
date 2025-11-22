namespace AmLink.Submissions.Mcp.Client.Configuration;

public static class ConfigurationValidator
{
    public static void ValidateAll(
        McpClientConfiguration? mcpConfig,
        IdentityServerConfiguration? idsConfig)
    {
        var errors = new List<string>();

        // MCP Server configuration validation
        ValidateMcpClientConfiguration(mcpConfig, errors);

        // Identity Server configuration validation
        ValidateIdentityServerConfiguration(idsConfig, errors);

        if (errors.Any())
        {
            var message = "Configuration validation failed:" + Environment.NewLine +
                         string.Join(Environment.NewLine, errors.Select(e => $"  - {e}"));
            throw new InvalidOperationException(message);
        }
    }

    private static void ValidateMcpClientConfiguration(McpClientConfiguration? config, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(config?.Url))
            errors.Add("McpServer:Url is required");
        else if (!IsValidHttpUrl(config.Url))
            errors.Add("McpServer:Url must be a valid absolute URL");

        // Optional BrowserUrl validation
        if (config?.BrowserUrl is not null && !IsValidHttpUrl(config.BrowserUrl))
            errors.Add("McpServer:BrowserUrl must be a valid absolute URL if provided");

        // Timeout validation
        if (config?.TimeoutSeconds <= 0)
            errors.Add("McpServer:TimeoutSeconds must be greater than 0");
    }

    private static void ValidateIdentityServerConfiguration(IdentityServerConfiguration? config, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(config?.Url))
            errors.Add("IdentityServer:Url is required");
        else if (!IsValidHttpUrl(config.Url))
            errors.Add("IdentityServer:Url must be a valid absolute URL");

        if (string.IsNullOrWhiteSpace(config?.ClientId))
            errors.Add("IdentityServer:ClientId is required");

        if (string.IsNullOrWhiteSpace(config?.ServerClientId))
            errors.Add("IdentityServer:ServerClientId is required");

        if (string.IsNullOrWhiteSpace(config?.ClientSecret))
            errors.Add("IdentityServer:ClientSecret is required");

        if (string.IsNullOrWhiteSpace(config?.GrantType))
            errors.Add("IdentityServer:GrantType is required");
        else if (config?.GrantType is not null)
        {
            var validGrantTypes = new[] { "authorization_code", "client_credentials", "password" };
            if (!validGrantTypes.Contains(config.GrantType))
                errors.Add($"IdentityServer:GrantType must be one of: {string.Join(", ", validGrantTypes)}");
        }

        if (string.IsNullOrWhiteSpace(config?.Scopes))
            errors.Add("IdentityServer:Scopes is required");

        if (string.IsNullOrWhiteSpace(config?.RedirectUri))
            errors.Add("IdentityServer:RedirectUri is required");
        else if (!IsValidHttpUrl(config.RedirectUri))
            errors.Add("IdentityServer:RedirectUri must be a valid absolute URL");

        // Validate ResponseMode if provided (not empty)
        if (!string.IsNullOrWhiteSpace(config?.ResponseMode))
        {
            var validResponseModes = new[] { "query", "fragment", "form_post" };
            if (!validResponseModes.Contains(config.ResponseMode))
                errors.Add($"IdentityServer:ResponseMode must be one of: {string.Join(", ", validResponseModes)}");
        }
    }

    private static bool IsValidHttpUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }
}
