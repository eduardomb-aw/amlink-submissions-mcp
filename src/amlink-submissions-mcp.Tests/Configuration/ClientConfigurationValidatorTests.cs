using AmLink.Submissions.Mcp.Client.Configuration;
using Xunit;

namespace AmLink.Submissions.Mcp.Tests.Configuration;

public class ClientConfigurationValidatorTests
{
    #region Valid Configuration Tests

    [Fact]
    public void ValidateAll_WithValidConfiguration_ShouldNotThrow()
    {
        // Arrange
        var mcpConfig = new McpClientConfiguration
        {
            Url = "https://mcp.example.com",
            BrowserUrl = "https://browser.example.com",
            Name = "Test MCP Server",
            TimeoutSeconds = 30
        };

        var idsConfig = new IdentityServerConfiguration
        {
            Url = "https://ids.example.com",
            ClientId = "test-client",
            ServerClientId = "test-server-client",
            ClientSecret = "test-secret",
            GrantType = "authorization_code",
            Scopes = "scope1 scope2",
            RedirectUri = "https://client.example.com/oauth/callback",
            ResponseMode = "query"
        };

        // Act & Assert
        var exception = Record.Exception(() =>
            ConfigurationValidator.ValidateAll(mcpConfig, idsConfig));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateAll_WithMinimalValidConfiguration_ShouldNotThrow()
    {
        // Arrange - Only required fields
        var mcpConfig = new McpClientConfiguration
        {
            Url = "https://mcp.example.com",
            TimeoutSeconds = 30
        };

        var idsConfig = new IdentityServerConfiguration
        {
            Url = "https://ids.example.com",
            ClientId = "test-client",
            ServerClientId = "test-server-client",
            ClientSecret = "test-secret",
            GrantType = "authorization_code",
            Scopes = "scope1",
            RedirectUri = "https://client.example.com/oauth/callback",
            ResponseMode = "query"
        };

        // Act & Assert
        var exception = Record.Exception(() =>
            ConfigurationValidator.ValidateAll(mcpConfig, idsConfig));

        Assert.Null(exception);
    }

    #endregion

    #region MCP Client Configuration Tests

    [Fact]
    public void ValidateAll_WithNullMcpConfig_ShouldThrow()
    {
        // Arrange
        var idsConfig = CreateValidIdentityServerConfiguration();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(null, idsConfig));

        Assert.Contains("McpServer:Url is required", exception.Message);
    }

    [Fact]
    public void ValidateAll_WithNullMcpServerUrl_ShouldThrow()
    {
        // Arrange
        var mcpConfig = new McpClientConfiguration
        {
            Url = null!,
            TimeoutSeconds = 30
        };
        var idsConfig = CreateValidIdentityServerConfiguration();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(mcpConfig, idsConfig));

        Assert.Contains("McpServer:Url is required", exception.Message);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("relative/path")]
    public void ValidateAll_WithInvalidMcpServerUrl_ShouldThrow(string invalidUrl)
    {
        // Arrange
        var mcpConfig = new McpClientConfiguration
        {
            Url = invalidUrl,
            TimeoutSeconds = 30
        };
        var idsConfig = CreateValidIdentityServerConfiguration();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(mcpConfig, idsConfig));

        Assert.Contains("McpServer:Url must be a valid absolute URL", exception.Message);
    }

    [Fact]
    public void ValidateAll_WithEmptyMcpServerUrl_ShouldThrow()
    {
        // Arrange
        var mcpConfig = new McpClientConfiguration
        {
            Url = "",
            TimeoutSeconds = 30
        };
        var idsConfig = CreateValidIdentityServerConfiguration();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(mcpConfig, idsConfig));

        Assert.Contains("McpServer:Url is required", exception.Message);
    }

    [Fact]
    public void ValidateAll_WithInvalidBrowserUrl_ShouldThrow()
    {
        // Arrange
        var mcpConfig = new McpClientConfiguration
        {
            Url = "https://mcp.example.com",
            BrowserUrl = "not-a-valid-url",
            TimeoutSeconds = 30
        };
        var idsConfig = CreateValidIdentityServerConfiguration();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(mcpConfig, idsConfig));

        Assert.Contains("McpServer:BrowserUrl must be a valid absolute URL if provided", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ValidateAll_WithInvalidTimeoutSeconds_ShouldThrow(int timeout)
    {
        // Arrange
        var mcpConfig = new McpClientConfiguration
        {
            Url = "https://mcp.example.com",
            TimeoutSeconds = timeout
        };
        var idsConfig = CreateValidIdentityServerConfiguration();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(mcpConfig, idsConfig));

        Assert.Contains("McpServer:TimeoutSeconds must be greater than 0", exception.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(300)]
    public void ValidateAll_WithValidTimeoutSeconds_ShouldNotThrow(int timeout)
    {
        // Arrange
        var mcpConfig = new McpClientConfiguration
        {
            Url = "https://mcp.example.com",
            TimeoutSeconds = timeout
        };
        var idsConfig = CreateValidIdentityServerConfiguration();

        // Act & Assert
        var exception = Record.Exception(() =>
            ConfigurationValidator.ValidateAll(mcpConfig, idsConfig));

        Assert.Null(exception);
    }

    #endregion

    #region Identity Server Configuration Tests

    [Fact]
    public void ValidateAll_WithNullIdentityServerConfig_ShouldThrow()
    {
        // Arrange
        var mcpConfig = CreateValidMcpClientConfiguration();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(mcpConfig, null));

        Assert.Contains("IdentityServer:Url is required", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAll_WithMissingIdentityServerUrl_ShouldThrow(string? url)
    {
        // Arrange
        var mcpConfig = CreateValidMcpClientConfiguration();
        var idsConfig = new IdentityServerConfiguration
        {
            Url = url!,
            ClientId = "client-id",
            ServerClientId = "server-client-id",
            ClientSecret = "secret",
            GrantType = "authorization_code",
            Scopes = "scopes",
            RedirectUri = "https://example.com/callback"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(mcpConfig, idsConfig));

        Assert.Contains("IdentityServer:Url is required", exception.Message);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("relative/path")]
    public void ValidateAll_WithInvalidIdentityServerUrl_ShouldThrow(string invalidUrl)
    {
        // Arrange
        var mcpConfig = CreateValidMcpClientConfiguration();
        var idsConfig = new IdentityServerConfiguration
        {
            Url = invalidUrl,
            ClientId = "client-id",
            ServerClientId = "server-client-id",
            ClientSecret = "secret",
            GrantType = "authorization_code",
            Scopes = "scopes",
            RedirectUri = "https://example.com/callback"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(mcpConfig, idsConfig));

        Assert.Contains("IdentityServer:Url must be a valid absolute URL", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAll_WithMissingClientId_ShouldThrow(string? clientId)
    {
        // Arrange
        var mcpConfig = CreateValidMcpClientConfiguration();
        var idsConfig = new IdentityServerConfiguration
        {
            Url = "https://ids.example.com",
            ClientId = clientId!,
            ServerClientId = "server-client-id",
            ClientSecret = "secret",
            GrantType = "authorization_code",
            Scopes = "scopes",
            RedirectUri = "https://example.com/callback"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(mcpConfig, idsConfig));

        Assert.Contains("IdentityServer:ClientId is required", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAll_WithMissingServerClientId_ShouldThrow(string? serverClientId)
    {
        // Arrange
        var mcpConfig = CreateValidMcpClientConfiguration();
        var idsConfig = new IdentityServerConfiguration
        {
            Url = "https://ids.example.com",
            ClientId = "client-id",
            ServerClientId = serverClientId!,
            ClientSecret = "secret",
            GrantType = "authorization_code",
            Scopes = "scopes",
            RedirectUri = "https://example.com/callback"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(mcpConfig, idsConfig));

        Assert.Contains("IdentityServer:ServerClientId is required", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAll_WithMissingClientSecret_ShouldThrow(string? clientSecret)
    {
        // Arrange
        var mcpConfig = CreateValidMcpClientConfiguration();
        var idsConfig = new IdentityServerConfiguration
        {
            Url = "https://ids.example.com",
            ClientId = "client-id",
            ServerClientId = "server-client-id",
            ClientSecret = clientSecret!,
            GrantType = "authorization_code",
            Scopes = "scopes",
            RedirectUri = "https://example.com/callback"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(mcpConfig, idsConfig));

        Assert.Contains("IdentityServer:ClientSecret is required", exception.Message);
    }

    [Theory]
    [InlineData("authorization_code")]
    [InlineData("client_credentials")]
    [InlineData("password")]
    public void ValidateAll_WithValidGrantTypes_ShouldNotThrow(string grantType)
    {
        // Arrange
        var mcpConfig = CreateValidMcpClientConfiguration();
        var idsConfig = new IdentityServerConfiguration
        {
            Url = "https://ids.example.com",
            ClientId = "client-id",
            ServerClientId = "server-client-id",
            ClientSecret = "secret",
            GrantType = grantType,
            Scopes = "scopes",
            RedirectUri = "https://example.com/callback"
        };

        // Act & Assert
        var exception = Record.Exception(() =>
            ConfigurationValidator.ValidateAll(mcpConfig, idsConfig));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("invalid_grant")]
    [InlineData("AUTHORIZATION_CODE")]
    [InlineData("implicit")]
    public void ValidateAll_WithInvalidGrantType_ShouldThrow(string invalidGrantType)
    {
        // Arrange
        var mcpConfig = CreateValidMcpClientConfiguration();
        var idsConfig = new IdentityServerConfiguration
        {
            Url = "https://ids.example.com",
            ClientId = "client-id",
            ServerClientId = "server-client-id",
            ClientSecret = "secret",
            GrantType = invalidGrantType,
            Scopes = "scopes",
            RedirectUri = "https://example.com/callback"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(mcpConfig, idsConfig));

        Assert.Contains("IdentityServer:GrantType must be one of:", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAll_WithMissingScopes_ShouldThrow(string? scopes)
    {
        // Arrange
        var mcpConfig = CreateValidMcpClientConfiguration();
        var idsConfig = new IdentityServerConfiguration
        {
            Url = "https://ids.example.com",
            ClientId = "client-id",
            ServerClientId = "server-client-id",
            ClientSecret = "secret",
            GrantType = "authorization_code",
            Scopes = scopes!,
            RedirectUri = "https://example.com/callback"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(mcpConfig, idsConfig));

        Assert.Contains("IdentityServer:Scopes is required", exception.Message);
    }

    [Fact]
    public void ValidateAll_WithNullRedirectUri_ShouldThrow()
    {
        // Arrange
        var mcpConfig = CreateValidMcpClientConfiguration();
        var idsConfig = new IdentityServerConfiguration
        {
            Url = "https://ids.example.com",
            ClientId = "client-id",
            ServerClientId = "server-client-id",
            ClientSecret = "secret",
            GrantType = "authorization_code",
            Scopes = "scopes",
            RedirectUri = null!
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(mcpConfig, idsConfig));

        Assert.Contains("IdentityServer:RedirectUri is required", exception.Message);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("relative/path")]
    public void ValidateAll_WithInvalidRedirectUri_ShouldThrow(string invalidUrl)
    {
        // Arrange
        var mcpConfig = CreateValidMcpClientConfiguration();
        var idsConfig = new IdentityServerConfiguration
        {
            Url = "https://ids.example.com",
            ClientId = "client-id",
            ServerClientId = "server-client-id",
            ClientSecret = "secret",
            GrantType = "authorization_code",
            Scopes = "scopes",
            RedirectUri = invalidUrl
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(mcpConfig, idsConfig));

        Assert.Contains("IdentityServer:RedirectUri must be a valid absolute URL", exception.Message);
    }

    [Theory]
    [InlineData("query")]
    [InlineData("fragment")]
    [InlineData("form_post")]
    public void ValidateAll_WithValidResponseModes_ShouldNotThrow(string responseMode)
    {
        // Arrange
        var mcpConfig = CreateValidMcpClientConfiguration();
        var idsConfig = new IdentityServerConfiguration
        {
            Url = "https://ids.example.com",
            ClientId = "client-id",
            ServerClientId = "server-client-id",
            ClientSecret = "secret",
            GrantType = "authorization_code",
            Scopes = "scopes",
            RedirectUri = "https://example.com/callback",
            ResponseMode = responseMode
        };

        // Act & Assert
        var exception = Record.Exception(() =>
            ConfigurationValidator.ValidateAll(mcpConfig, idsConfig));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("invalid_mode")]
    [InlineData("QUERY")]
    [InlineData("token")]
    public void ValidateAll_WithInvalidResponseMode_ShouldThrow(string invalidResponseMode)
    {
        // Arrange
        var mcpConfig = CreateValidMcpClientConfiguration();
        var idsConfig = new IdentityServerConfiguration
        {
            Url = "https://ids.example.com",
            ClientId = "client-id",
            ServerClientId = "server-client-id",
            ClientSecret = "secret",
            GrantType = "authorization_code",
            Scopes = "scopes",
            RedirectUri = "https://example.com/callback",
            ResponseMode = invalidResponseMode
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(mcpConfig, idsConfig));

        Assert.Contains("IdentityServer:ResponseMode must be one of:", exception.Message);
    }

    #endregion

    #region Multiple Errors Tests

    [Fact]
    public void ValidateAll_WithMultipleErrors_ShouldAggregateAllErrors()
    {
        // Arrange - Multiple invalid configurations
        var mcpConfig = new McpClientConfiguration
        {
            Url = "not-a-url",
            BrowserUrl = "invalid-url",
            TimeoutSeconds = -1
        };
        var idsConfig = new IdentityServerConfiguration
        {
            Url = "invalid-url",
            ClientId = "",
            ServerClientId = "   ",
            ClientSecret = "",
            GrantType = "invalid_grant",
            Scopes = "",
            RedirectUri = "not-a-url",
            ResponseMode = "invalid_mode"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(mcpConfig, idsConfig));

        // Verify multiple errors are reported
        Assert.Contains("McpServer:Url must be a valid absolute URL", exception.Message);
        Assert.Contains("McpServer:BrowserUrl must be a valid absolute URL if provided", exception.Message);
        Assert.Contains("McpServer:TimeoutSeconds must be greater than 0", exception.Message);
        Assert.Contains("IdentityServer:Url must be a valid absolute URL", exception.Message);
        Assert.Contains("IdentityServer:ClientId is required", exception.Message);
        Assert.Contains("IdentityServer:ServerClientId is required", exception.Message);
        Assert.Contains("IdentityServer:ClientSecret is required", exception.Message);
        Assert.Contains("IdentityServer:GrantType must be one of:", exception.Message);
        Assert.Contains("IdentityServer:Scopes is required", exception.Message);
        Assert.Contains("IdentityServer:RedirectUri must be a valid absolute URL", exception.Message);
        Assert.Contains("IdentityServer:ResponseMode must be one of:", exception.Message);
    }

    #endregion

    #region Helper Methods

    private static McpClientConfiguration CreateValidMcpClientConfiguration()
    {
        return new McpClientConfiguration
        {
            Url = "https://mcp.example.com",
            TimeoutSeconds = 30
        };
    }

    private static IdentityServerConfiguration CreateValidIdentityServerConfiguration()
    {
        return new IdentityServerConfiguration
        {
            Url = "https://ids.example.com",
            ClientId = "test-client",
            ServerClientId = "test-server-client",
            ClientSecret = "test-secret",
            GrantType = "authorization_code",
            Scopes = "scope1 scope2",
            RedirectUri = "https://client.example.com/oauth/callback",
            ResponseMode = "query"
        };
    }

    #endregion
}
