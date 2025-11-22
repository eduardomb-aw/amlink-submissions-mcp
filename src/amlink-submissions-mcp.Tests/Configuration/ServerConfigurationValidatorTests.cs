using AmLink.Submission.Mcp.Server.Configuration;
using Xunit;

namespace AmLink.Submissions.Mcp.Tests.Configuration;

public class ServerConfigurationValidatorTests
{
    #region Valid Configuration Tests

    [Fact]
    public void ValidateAll_WithValidConfiguration_ShouldNotThrow()
    {
        // Arrange
        var serverConfig = new ServerConfiguration
        {
            Url = "https://server.example.com",
            ResourceBaseUri = "https://resource.example.com",
            ResourceDocumentationUrl = "https://docs.example.com"
        };

        var idsConfig = new IdentityServerConfiguration
        {
            Url = "https://ids.example.com",
            ClientId = "test-client",
            ClientSecret = "test-secret",
            GrantType = "authorization_code",
            Scopes = "scope1 scope2"
        };

        var externalApisConfig = new ExternalApisConfiguration
        {
            SubmissionApi = new SubmissionApiConfiguration
            {
                BaseUrl = "https://api.example.com",
                RequiredScope = "api-scope",
                UserAgent = "test-agent",
                Version = "1.0"
            }
        };

        // Act & Assert
        var exception = Record.Exception(() =>
            ConfigurationValidator.ValidateAll(serverConfig, idsConfig, externalApisConfig));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateAll_WithMinimalValidConfiguration_ShouldNotThrow()
    {
        // Arrange - Only required fields
        var serverConfig = new ServerConfiguration
        {
            Url = "https://server.example.com"
        };

        var idsConfig = new IdentityServerConfiguration
        {
            Url = "https://ids.example.com",
            ClientId = "test-client",
            ClientSecret = "test-secret",
            GrantType = "client_credentials",
            Scopes = "api-scope"
        };

        var externalApisConfig = new ExternalApisConfiguration
        {
            SubmissionApi = new SubmissionApiConfiguration
            {
                BaseUrl = "https://api.example.com",
                RequiredScope = "api-scope",
                UserAgent = "test-agent"
            }
        };

        // Act & Assert
        var exception = Record.Exception(() =>
            ConfigurationValidator.ValidateAll(serverConfig, idsConfig, externalApisConfig));

        Assert.Null(exception);
    }

    #endregion

    #region Server Configuration Tests

    [Fact]
    public void ValidateAll_WithNullServerConfig_ShouldThrow()
    {
        // Arrange
        var idsConfig = CreateValidIdentityServerConfiguration();
        var externalApisConfig = CreateValidExternalApisConfiguration();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(null, idsConfig, externalApisConfig));

        Assert.Contains("Server:Url is required", exception.Message);
    }

    [Fact]
    public void ValidateAll_WithNullServerUrl_ShouldThrow()
    {
        // Arrange
        var serverConfig = new ServerConfiguration { Url = null! };
        var idsConfig = CreateValidIdentityServerConfiguration();
        var externalApisConfig = CreateValidExternalApisConfiguration();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(serverConfig, idsConfig, externalApisConfig));

        Assert.Contains("Server:Url is required", exception.Message);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("relative/path")]
    [InlineData("ftp://invalid-scheme.com")]
    public void ValidateAll_WithInvalidServerUrl_ShouldThrow(string invalidUrl)
    {
        // Arrange
        var serverConfig = new ServerConfiguration { Url = invalidUrl };
        var idsConfig = CreateValidIdentityServerConfiguration();
        var externalApisConfig = CreateValidExternalApisConfiguration();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(serverConfig, idsConfig, externalApisConfig));

        Assert.Contains("Server:Url must be a valid absolute URL", exception.Message);
    }

    [Fact]
    public void ValidateAll_WithEmptyServerUrl_ShouldThrow()
    {
        // Arrange
        var serverConfig = new ServerConfiguration { Url = "" };
        var idsConfig = CreateValidIdentityServerConfiguration();
        var externalApisConfig = CreateValidExternalApisConfiguration();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(serverConfig, idsConfig, externalApisConfig));

        Assert.Contains("Server:Url is required", exception.Message);
    }

    [Fact]
    public void ValidateAll_WithInvalidResourceBaseUri_ShouldThrow()
    {
        // Arrange
        var serverConfig = new ServerConfiguration
        {
            Url = "https://server.example.com",
            ResourceBaseUri = "not-a-valid-url"
        };
        var idsConfig = CreateValidIdentityServerConfiguration();
        var externalApisConfig = CreateValidExternalApisConfiguration();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(serverConfig, idsConfig, externalApisConfig));

        Assert.Contains("Server:ResourceBaseUri must be a valid absolute URL if provided", exception.Message);
    }

    [Fact]
    public void ValidateAll_WithInvalidResourceDocumentationUrl_ShouldThrow()
    {
        // Arrange
        var serverConfig = new ServerConfiguration
        {
            Url = "https://server.example.com",
            ResourceDocumentationUrl = "invalid-url"
        };
        var idsConfig = CreateValidIdentityServerConfiguration();
        var externalApisConfig = CreateValidExternalApisConfiguration();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(serverConfig, idsConfig, externalApisConfig));

        Assert.Contains("Server:ResourceDocumentationUrl must be a valid absolute URL if provided", exception.Message);
    }

    #endregion

    #region Identity Server Configuration Tests

    [Fact]
    public void ValidateAll_WithNullIdentityServerConfig_ShouldThrow()
    {
        // Arrange
        var serverConfig = CreateValidServerConfiguration();
        var externalApisConfig = CreateValidExternalApisConfiguration();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(serverConfig, null, externalApisConfig));

        Assert.Contains("IdentityServer:Url is required", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAll_WithMissingIdentityServerUrl_ShouldThrow(string? url)
    {
        // Arrange
        var serverConfig = CreateValidServerConfiguration();
        var idsConfig = new IdentityServerConfiguration
        {
            Url = url!,
            ClientId = "client-id",
            ClientSecret = "secret",
            GrantType = "authorization_code",
            Scopes = "scopes"
        };
        var externalApisConfig = CreateValidExternalApisConfiguration();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(serverConfig, idsConfig, externalApisConfig));

        Assert.Contains("IdentityServer:Url is required", exception.Message);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("relative/path")]
    public void ValidateAll_WithInvalidIdentityServerUrl_ShouldThrow(string invalidUrl)
    {
        // Arrange
        var serverConfig = CreateValidServerConfiguration();
        var idsConfig = new IdentityServerConfiguration
        {
            Url = invalidUrl,
            ClientId = "client-id",
            ClientSecret = "secret",
            GrantType = "authorization_code",
            Scopes = "scopes"
        };
        var externalApisConfig = CreateValidExternalApisConfiguration();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(serverConfig, idsConfig, externalApisConfig));

        Assert.Contains("IdentityServer:Url must be a valid absolute URL", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAll_WithMissingClientId_ShouldThrow(string? clientId)
    {
        // Arrange
        var serverConfig = CreateValidServerConfiguration();
        var idsConfig = new IdentityServerConfiguration
        {
            Url = "https://ids.example.com",
            ClientId = clientId!,
            ClientSecret = "secret",
            GrantType = "authorization_code",
            Scopes = "scopes"
        };
        var externalApisConfig = CreateValidExternalApisConfiguration();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(serverConfig, idsConfig, externalApisConfig));

        Assert.Contains("IdentityServer:ClientId is required", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAll_WithMissingClientSecret_ShouldThrow(string? clientSecret)
    {
        // Arrange
        var serverConfig = CreateValidServerConfiguration();
        var idsConfig = new IdentityServerConfiguration
        {
            Url = "https://ids.example.com",
            ClientId = "client-id",
            ClientSecret = clientSecret!,
            GrantType = "authorization_code",
            Scopes = "scopes"
        };
        var externalApisConfig = CreateValidExternalApisConfiguration();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(serverConfig, idsConfig, externalApisConfig));

        Assert.Contains("IdentityServer:ClientSecret is required", exception.Message);
    }

    [Theory]
    [InlineData("authorization_code")]
    [InlineData("client_credentials")]
    [InlineData("password")]
    public void ValidateAll_WithValidGrantTypes_ShouldNotThrow(string grantType)
    {
        // Arrange
        var serverConfig = CreateValidServerConfiguration();
        var idsConfig = new IdentityServerConfiguration
        {
            Url = "https://ids.example.com",
            ClientId = "client-id",
            ClientSecret = "secret",
            GrantType = grantType,
            Scopes = "scopes"
        };
        var externalApisConfig = CreateValidExternalApisConfiguration();

        // Act & Assert
        var exception = Record.Exception(() =>
            ConfigurationValidator.ValidateAll(serverConfig, idsConfig, externalApisConfig));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("invalid_grant")]
    [InlineData("AUTHORIZATION_CODE")]
    [InlineData("implicit")]
    public void ValidateAll_WithInvalidGrantType_ShouldThrow(string invalidGrantType)
    {
        // Arrange
        var serverConfig = CreateValidServerConfiguration();
        var idsConfig = new IdentityServerConfiguration
        {
            Url = "https://ids.example.com",
            ClientId = "client-id",
            ClientSecret = "secret",
            GrantType = invalidGrantType,
            Scopes = "scopes"
        };
        var externalApisConfig = CreateValidExternalApisConfiguration();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(serverConfig, idsConfig, externalApisConfig));

        Assert.Contains("IdentityServer:GrantType must be one of:", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAll_WithMissingScopes_ShouldThrow(string? scopes)
    {
        // Arrange
        var serverConfig = CreateValidServerConfiguration();
        var idsConfig = new IdentityServerConfiguration
        {
            Url = "https://ids.example.com",
            ClientId = "client-id",
            ClientSecret = "secret",
            GrantType = "authorization_code",
            Scopes = scopes!
        };
        var externalApisConfig = CreateValidExternalApisConfiguration();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(serverConfig, idsConfig, externalApisConfig));

        Assert.Contains("IdentityServer:Scopes is required", exception.Message);
    }

    #endregion

    #region External APIs Configuration Tests

    [Fact]
    public void ValidateAll_WithNullExternalApisConfig_ShouldThrow()
    {
        // Arrange
        var serverConfig = CreateValidServerConfiguration();
        var idsConfig = CreateValidIdentityServerConfiguration();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(serverConfig, idsConfig, null));

        Assert.Contains("ExternalApis:SubmissionApi:BaseUrl is required", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ValidateAll_WithMissingSubmissionApiBaseUrl_ShouldThrow(string? baseUrl)
    {
        // Arrange
        var serverConfig = CreateValidServerConfiguration();
        var idsConfig = CreateValidIdentityServerConfiguration();
        var externalApisConfig = new ExternalApisConfiguration
        {
            SubmissionApi = new SubmissionApiConfiguration
            {
                BaseUrl = baseUrl!,
                RequiredScope = "api-scope",
                UserAgent = "test-agent"
            }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(serverConfig, idsConfig, externalApisConfig));

        Assert.Contains("ExternalApis:SubmissionApi:BaseUrl is required", exception.Message);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("relative/path")]
    public void ValidateAll_WithInvalidSubmissionApiBaseUrl_ShouldThrow(string invalidUrl)
    {
        // Arrange
        var serverConfig = CreateValidServerConfiguration();
        var idsConfig = CreateValidIdentityServerConfiguration();
        var externalApisConfig = new ExternalApisConfiguration
        {
            SubmissionApi = new SubmissionApiConfiguration
            {
                BaseUrl = invalidUrl,
                RequiredScope = "api-scope",
                UserAgent = "test-agent"
            }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(serverConfig, idsConfig, externalApisConfig));

        Assert.Contains("ExternalApis:SubmissionApi:BaseUrl must be a valid absolute URL", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAll_WithMissingRequiredScope_ShouldThrow(string? requiredScope)
    {
        // Arrange
        var serverConfig = CreateValidServerConfiguration();
        var idsConfig = CreateValidIdentityServerConfiguration();
        var externalApisConfig = new ExternalApisConfiguration
        {
            SubmissionApi = new SubmissionApiConfiguration
            {
                BaseUrl = "https://api.example.com",
                RequiredScope = requiredScope!,
                UserAgent = "test-agent"
            }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(serverConfig, idsConfig, externalApisConfig));

        Assert.Contains("ExternalApis:SubmissionApi:RequiredScope is required", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAll_WithMissingUserAgent_ShouldThrow(string? userAgent)
    {
        // Arrange
        var serverConfig = CreateValidServerConfiguration();
        var idsConfig = CreateValidIdentityServerConfiguration();
        var externalApisConfig = new ExternalApisConfiguration
        {
            SubmissionApi = new SubmissionApiConfiguration
            {
                BaseUrl = "https://api.example.com",
                RequiredScope = "api-scope",
                UserAgent = userAgent!
            }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(serverConfig, idsConfig, externalApisConfig));

        Assert.Contains("ExternalApis:SubmissionApi:UserAgent is required", exception.Message);
    }

    #endregion

    #region Multiple Errors Tests

    [Fact]
    public void ValidateAll_WithMultipleErrors_ShouldAggregateAllErrors()
    {
        // Arrange - Multiple invalid configurations
        var serverConfig = new ServerConfiguration { Url = "not-a-url" };
        var idsConfig = new IdentityServerConfiguration
        {
            Url = "invalid-url",
            ClientId = "",
            ClientSecret = "secret",
            GrantType = "invalid_grant",
            Scopes = ""
        };
        var externalApisConfig = new ExternalApisConfiguration
        {
            SubmissionApi = new SubmissionApiConfiguration
            {
                BaseUrl = "not-a-url",
                RequiredScope = "",
                UserAgent = ""
            }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationValidator.ValidateAll(serverConfig, idsConfig, externalApisConfig));

        // Verify multiple errors are reported
        Assert.Contains("Server:Url must be a valid absolute URL", exception.Message);
        Assert.Contains("IdentityServer:Url must be a valid absolute URL", exception.Message);
        Assert.Contains("IdentityServer:ClientId is required", exception.Message);
        Assert.Contains("IdentityServer:GrantType must be one of:", exception.Message);
        Assert.Contains("IdentityServer:Scopes is required", exception.Message);
        Assert.Contains("ExternalApis:SubmissionApi:BaseUrl must be a valid absolute URL", exception.Message);
        Assert.Contains("ExternalApis:SubmissionApi:RequiredScope is required", exception.Message);
        Assert.Contains("ExternalApis:SubmissionApi:UserAgent is required", exception.Message);
    }

    #endregion

    #region Helper Methods

    private static ServerConfiguration CreateValidServerConfiguration()
    {
        return new ServerConfiguration
        {
            Url = "https://server.example.com"
        };
    }

    private static IdentityServerConfiguration CreateValidIdentityServerConfiguration()
    {
        return new IdentityServerConfiguration
        {
            Url = "https://ids.example.com",
            ClientId = "test-client",
            ClientSecret = "test-secret",
            GrantType = "authorization_code",
            Scopes = "scope1 scope2"
        };
    }

    private static ExternalApisConfiguration CreateValidExternalApisConfiguration()
    {
        return new ExternalApisConfiguration
        {
            SubmissionApi = new SubmissionApiConfiguration
            {
                BaseUrl = "https://api.example.com",
                RequiredScope = "api-scope",
                UserAgent = "test-agent",
                Version = "1.0"
            }
        };
    }

    #endregion
}
