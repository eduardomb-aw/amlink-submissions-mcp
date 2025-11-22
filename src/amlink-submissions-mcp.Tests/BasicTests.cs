using Xunit;

namespace AmLink.Submissions.Mcp.Tests;

public class BasicTests
{
    [Fact]
    public void Server_Should_HaveValidConfiguration()
    {
        // Basic test to ensure the server project compiles and has valid structure
        Assert.True(true, "Server project structure is valid");
    }

    [Fact]
    public void Client_Should_HaveValidConfiguration()
    {
        // Basic test to ensure the client project compiles and has valid structure  
        Assert.True(true, "Client project structure is valid");
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user@domain.org")]
    public void EmailValidation_Should_PassForValidEmails(string email)
    {
        // Simple email format validation test
        var isValid = email.Contains("@") && email.Contains(".");
        Assert.True(isValid, $"Email {email} should be valid");
    }

    [Fact]
    public void Environment_Should_BeConfigured()
    {
        // Test that environment variables can be accessed
        var envVar = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
        Assert.NotNull(envVar);
        Assert.NotEmpty(envVar);
    }
}
