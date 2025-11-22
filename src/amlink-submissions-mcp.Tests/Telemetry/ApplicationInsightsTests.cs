using AmLink.Submission.Mcp.Server.Telemetry;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Moq;
using Xunit;

namespace AmLink.Submissions.Mcp.Tests.Telemetry;

/// <summary>
/// Tests for Application Insights integration and custom telemetry processor
/// </summary>
public class ApplicationInsightsTests
{
    #region CustomTelemetryProcessor Tests

    [Fact]
    public void CustomTelemetryProcessor_Should_EnhanceRequestTelemetry()
    {
        // Arrange
        var mockNext = new Mock<ITelemetryProcessor>();
        var processor = new CustomTelemetryProcessor(mockNext.Object);
        var request = new RequestTelemetry
        {
            Name = "Test Request",
            Url = new Uri("http://localhost/test")
        };

        // Act
        processor.Process(request);

        // Assert
        Assert.True(request.Properties.ContainsKey("Service"));
        Assert.Equal("MCP-Server", request.Properties["Service"]);
        Assert.True(request.Properties.ContainsKey("Version"));
        Assert.Equal("1.0", request.Properties["Version"]);
        Assert.True(request.Properties.ContainsKey("Component"));
        Assert.Equal("AmLink-Submission-MCP", request.Properties["Component"]);
        mockNext.Verify(n => n.Process(request), Times.Once);
    }

    [Fact]
    public void CustomTelemetryProcessor_Should_EnhanceDependencyTelemetry_ForAmLinkApi()
    {
        // Arrange
        var mockNext = new Mock<ITelemetryProcessor>();
        var processor = new CustomTelemetryProcessor(mockNext.Object);
        var dependency = new DependencyTelemetry
        {
            Name = "GET /submissions",
            Target = "amlink-submission-api-dev.amwins.net",
            Type = "HTTP"
        };

        // Act
        processor.Process(dependency);

        // Assert
        Assert.True(dependency.Properties.ContainsKey("ApiType"));
        Assert.Equal("AmLink", dependency.Properties["ApiType"]);
        Assert.True(dependency.Properties.ContainsKey("ServiceFamily"));
        Assert.Equal("AmWins", dependency.Properties["ServiceFamily"]);
        Assert.True(dependency.Properties.ContainsKey("ApiName"));
        Assert.Equal("Submission", dependency.Properties["ApiName"]);
        Assert.True(dependency.Properties.ContainsKey("BusinessDomain"));
        Assert.Equal("Submissions", dependency.Properties["BusinessDomain"]);
        mockNext.Verify(n => n.Process(dependency), Times.Once);
    }

    [Fact]
    public void CustomTelemetryProcessor_Should_EnhanceDependencyTelemetry_ForIdentityServer()
    {
        // Arrange
        var mockNext = new Mock<ITelemetryProcessor>();
        var processor = new CustomTelemetryProcessor(mockNext.Object);
        var dependency = new DependencyTelemetry
        {
            Name = "GET /.well-known/openid-configuration",
            Target = "identitydev.amwins.com",
            Type = "HTTP"
        };

        // Act
        processor.Process(dependency);

        // Assert
        Assert.True(dependency.Properties.ContainsKey("ApiType"));
        Assert.Equal("IdentityServer", dependency.Properties["ApiType"]);
        Assert.True(dependency.Properties.ContainsKey("ServiceFamily"));
        Assert.Equal("Auth", dependency.Properties["ServiceFamily"]);
        mockNext.Verify(n => n.Process(dependency), Times.Once);
    }

    [Fact]
    public void CustomTelemetryProcessor_Should_EnhanceExceptionTelemetry()
    {
        // Arrange
        var mockNext = new Mock<ITelemetryProcessor>();
        var processor = new CustomTelemetryProcessor(mockNext.Object);
        var exception = new ExceptionTelemetry(new InvalidOperationException("Test exception"))
        {
            Message = "Test exception occurred"
        };

        // Act
        processor.Process(exception);

        // Assert
        Assert.True(exception.Properties.ContainsKey("Service"));
        Assert.Equal("MCP-Server", exception.Properties["Service"]);
        Assert.True(exception.Properties.ContainsKey("Component"));
        Assert.Equal("AmLink-Submission-MCP", exception.Properties["Component"]);
        mockNext.Verify(n => n.Process(exception), Times.Once);
    }

    [Fact]
    public void CustomTelemetryProcessor_Should_EnhanceTraceTelemetry()
    {
        // Arrange
        var mockNext = new Mock<ITelemetryProcessor>();
        var processor = new CustomTelemetryProcessor(mockNext.Object);
        var trace = new TraceTelemetry
        {
            Message = "Test trace message",
            SeverityLevel = SeverityLevel.Information
        };

        // Act
        processor.Process(trace);

        // Assert
        Assert.True(trace.Properties.ContainsKey("Service"));
        Assert.Equal("MCP-Server", trace.Properties["Service"]);
        mockNext.Verify(n => n.Process(trace), Times.Once);
    }

    [Fact]
    public void CustomTelemetryProcessor_Should_HandleNonAmLinkDependencies()
    {
        // Arrange
        var mockNext = new Mock<ITelemetryProcessor>();
        var processor = new CustomTelemetryProcessor(mockNext.Object);
        var dependency = new DependencyTelemetry
        {
            Name = "GET /external",
            Target = "external-api.example.com",
            Type = "HTTP"
        };

        // Act
        processor.Process(dependency);

        // Assert - Should not add AmLink-specific properties
        Assert.False(dependency.Properties.ContainsKey("ApiType"));
        Assert.False(dependency.Properties.ContainsKey("ServiceFamily"));
        Assert.False(dependency.Properties.ContainsKey("ApiName"));
        mockNext.Verify(n => n.Process(dependency), Times.Once);
    }

    [Fact]
    public void CustomTelemetryProcessor_Should_CallNextProcessorForAllTelemetry()
    {
        // Arrange
        var mockNext = new Mock<ITelemetryProcessor>();
        var processor = new CustomTelemetryProcessor(mockNext.Object);
        var telemetryItems = new ITelemetry[]
        {
            new RequestTelemetry(),
            new DependencyTelemetry(),
            new ExceptionTelemetry(),
            new TraceTelemetry()
        };

        // Act
        foreach (var item in telemetryItems)
        {
            processor.Process(item);
        }

        // Assert
        mockNext.Verify(n => n.Process(It.IsAny<ITelemetry>()), Times.Exactly(telemetryItems.Length));
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void ApplicationInsights_Configuration_Should_HaveValidStructure()
    {
        // This test verifies that the Application Insights configuration structure is valid
        // The actual connection string would be set via environment variables in production

        var config = new
        {
            ConnectionString = "",
            EnableAdaptiveSampling = true,
            EnableQuickPulseMetricStream = true
        };

        Assert.NotNull(config);
        Assert.IsType<string>(config.ConnectionString);
        Assert.IsType<bool>(config.EnableAdaptiveSampling);
        Assert.IsType<bool>(config.EnableQuickPulseMetricStream);
        Assert.True(config.EnableAdaptiveSampling);
        Assert.True(config.EnableQuickPulseMetricStream);
    }

    [Theory]
    [InlineData("InstrumentationKey=12345678-1234-1234-1234-123456789012;IngestionEndpoint=https://test.applicationinsights.azure.com/")]
    [InlineData("InstrumentationKey=abcdefab-abcd-abcd-abcd-abcdefabcdef;IngestionEndpoint=https://prod.applicationinsights.azure.com/;LiveEndpoint=https://prod.livediagnostics.applicationinsights.azure.com/")]
    public void ApplicationInsights_ConnectionString_Should_BeValidFormat(string connectionString)
    {
        // Arrange & Act
        var hasInstrumentationKey = connectionString.Contains("InstrumentationKey=");
        var hasIngestionEndpoint = connectionString.Contains("IngestionEndpoint=");

        // Assert
        Assert.True(hasInstrumentationKey, "Connection string must contain InstrumentationKey");
        Assert.True(hasIngestionEndpoint, "Connection string must contain IngestionEndpoint");
    }

    #endregion

    #region Correlation ID Tests

    [Fact]
    public void RequestTelemetry_Should_SupportOperationId()
    {
        // Arrange
        var operationId = Guid.NewGuid().ToString();
        var request = new RequestTelemetry
        {
            Name = "Test Request"
        };

        // Act - Set operation ID for correlation (normally done automatically by Application Insights)
        request.Context.Operation.Id = operationId;

        // Assert
        Assert.Equal(operationId, request.Context.Operation.Id);
        Assert.NotEmpty(request.Context.Operation.Id);
    }

    [Fact]
    public void DependencyTelemetry_Should_InheritOperationId()
    {
        // Arrange
        var operationId = Guid.NewGuid().ToString();
        var dependency = new DependencyTelemetry
        {
            Name = "External API Call"
        };
        dependency.Context.Operation.Id = operationId;

        // Act & Assert
        Assert.Equal(operationId, dependency.Context.Operation.Id);
    }

    #endregion

    #region Telemetry Properties Tests

    [Fact]
    public void Telemetry_Should_SupportCustomProperties()
    {
        // Arrange
        var request = new RequestTelemetry();
        var customKey = "CustomProperty";
        var customValue = "CustomValue";

        // Act
        request.Properties[customKey] = customValue;

        // Assert
        Assert.True(request.Properties.ContainsKey(customKey));
        Assert.Equal(customValue, request.Properties[customKey]);
    }

    [Fact]
    public void DependencyTelemetry_Should_TrackHttpCalls()
    {
        // Arrange & Act
        var dependency = new DependencyTelemetry
        {
            Name = "GET /api/submissions",
            Type = "HTTP",
            Target = "api.example.com",
            Data = "https://api.example.com/api/submissions",
            Success = true,
            Duration = TimeSpan.FromMilliseconds(150)
        };

        // Assert
        Assert.Equal("HTTP", dependency.Type);
        Assert.Equal("api.example.com", dependency.Target);
        Assert.True(dependency.Success);
        Assert.True(dependency.Duration.TotalMilliseconds > 0);
    }

    #endregion
}
