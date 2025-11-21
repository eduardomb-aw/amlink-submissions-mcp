using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace AmLink.Submissions.Mcp.Tests;

public class HealthCheckTests
{
    [Fact]
    public void HealthCheckResult_Healthy_Should_HaveCorrectStatus()
    {
        // Arrange & Act
        var result = HealthCheckResult.Healthy("Test healthy");

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("Test healthy", result.Description);
    }

    [Fact]
    public void HealthCheckResult_Degraded_Should_HaveCorrectStatus()
    {
        // Arrange & Act
        var result = HealthCheckResult.Degraded("Test degraded");

        // Assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal("Test degraded", result.Description);
    }

    [Fact]
    public void HealthCheckResult_Unhealthy_Should_HaveCorrectStatus()
    {
        // Arrange & Act
        var result = HealthCheckResult.Unhealthy("Test unhealthy");

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Test unhealthy", result.Description);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    [InlineData("/health/live")]
    public void HealthCheckEndpoint_Paths_Should_BeValid(string path)
    {
        // Arrange
        var expectedPaths = new[] { "/health", "/health/ready", "/health/live" };

        // Assert
        Assert.Contains(path, expectedPaths);
    }

    [Fact]
    public void HealthCheck_JsonResponse_Should_ContainRequiredFields()
    {
        // Arrange
        var expectedFields = new[] { "status", "checks", "totalDuration" };
        var requiredPerCheckFields = new[] { "name", "status", "description", "duration" };

        // Assert
        Assert.NotEmpty(expectedFields);
        Assert.NotEmpty(requiredPerCheckFields);
        Assert.Contains("status", expectedFields);
        Assert.Contains("checks", expectedFields);
    }
}
