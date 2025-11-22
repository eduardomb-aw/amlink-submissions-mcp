namespace AmLink.Submission.Mcp.Server.Configuration;

public static class ConfigurationValidator
{
    public static void ValidateAll(
        ServerConfiguration? serverConfig,
        IdentityServerConfiguration? idsConfig,
        ExternalApisConfiguration? externalApisConfig)
    {
        var errors = new List<string>();

        // Server configuration validation
        ValidateServerConfiguration(serverConfig, errors);

        // Identity Server configuration validation
        ValidateIdentityServerConfiguration(idsConfig, errors);

        // External APIs configuration validation
        ValidateExternalApisConfiguration(externalApisConfig, errors);

        if (errors.Any())
        {
            var message = "Configuration validation failed:" + Environment.NewLine +
                         string.Join(Environment.NewLine, errors.Select(e => $"  - {e}"));
            throw new InvalidOperationException(message);
        }
    }

    private static void ValidateServerConfiguration(ServerConfiguration? config, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(config?.Url))
            errors.Add("Server:Url is required");
        else if (!IsValidHttpUrl(config.Url))
            errors.Add("Server:Url must be a valid absolute URL");

        // Optional fields with URL validation
        if (config?.ResourceBaseUri is not null && !IsValidHttpUrl(config.ResourceBaseUri))
            errors.Add("Server:ResourceBaseUri must be a valid absolute URL if provided");

        if (config?.ResourceDocumentationUrl is not null && !IsValidHttpUrl(config.ResourceDocumentationUrl))
            errors.Add("Server:ResourceDocumentationUrl must be a valid absolute URL if provided");
    }

    private static void ValidateIdentityServerConfiguration(IdentityServerConfiguration? config, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(config?.Url))
            errors.Add("IdentityServer:Url is required");
        else if (!IsValidHttpUrl(config.Url))
            errors.Add("IdentityServer:Url must be a valid absolute URL");

        if (string.IsNullOrWhiteSpace(config?.ClientId))
            errors.Add("IdentityServer:ClientId is required");

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
    }

    private static void ValidateExternalApisConfiguration(ExternalApisConfiguration? config, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(config?.SubmissionApi?.BaseUrl))
            errors.Add("ExternalApis:SubmissionApi:BaseUrl is required");
        else if (!IsValidHttpUrl(config.SubmissionApi.BaseUrl))
            errors.Add("ExternalApis:SubmissionApi:BaseUrl must be a valid absolute URL");

        if (string.IsNullOrWhiteSpace(config?.SubmissionApi?.RequiredScope))
            errors.Add("ExternalApis:SubmissionApi:RequiredScope is required");

        if (string.IsNullOrWhiteSpace(config?.SubmissionApi?.UserAgent))
            errors.Add("ExternalApis:SubmissionApi:UserAgent is required");
    }

    private static bool IsValidHttpUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }
}
