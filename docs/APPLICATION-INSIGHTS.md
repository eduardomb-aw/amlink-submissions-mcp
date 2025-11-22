# Application Insights Integration Guide

This document describes the Application Insights integration for correlation IDs and enhanced observability in the AmLink Submissions MCP project.

## Overview

Application Insights provides automatic distributed tracing, correlation IDs, performance monitoring, and rich telemetry across the MCP server and client applications. The integration includes:

- **Automatic correlation IDs** for tracing requests across service boundaries
- **Distributed tracing** for visualizing request flows
- **Dependency tracking** for external API calls (AmLink APIs, Identity Server)
- **Performance metrics** and application monitoring
- **Exception tracking** with full context and stack traces
- **Custom telemetry enrichment** for better filtering and analysis

## Architecture

```text
┌─────────────────┐
│  MCP Client     │
│  Application    │──────┐
└─────────────────┘      │
                         │  Correlation IDs
┌─────────────────┐      │  & Telemetry
│  MCP Server     │──────┤
│  Application    │      │
└─────────────────┘      │
                         │
┌─────────────────┐      │
│  External APIs  │      │
│  (AmLink, etc)  │──────┘
└─────────────────┘
         │
         ↓
┌─────────────────────────────────┐
│   Application Insights          │
│   (Azure Monitor)                │
├─────────────────────────────────┤
│  - Request Tracking              │
│  - Dependency Tracking           │
│  - Exception Tracking            │
│  - Performance Metrics           │
│  - Custom Properties             │
└─────────────────────────────────┘
         │
         ↓
┌─────────────────────────────────┐
│   Log Analytics Workspace       │
│   (90-day retention)             │
└─────────────────────────────────┘
```

## Configuration

### Connection String

Application Insights is configured via connection string in both server and client applications:

**Environment Variable:**
```bash
ConnectionStrings__ApplicationInsights="InstrumentationKey=<key>;IngestionEndpoint=https://<region>.applicationinsights.azure.com/"
```

**appsettings.json:**
```json
{
  "ApplicationInsights": {
    "ConnectionString": "",
    "EnableAdaptiveSampling": true,
    "EnableQuickPulseMetricStream": true
  }
}
```

### Infrastructure Deployment

Application Insights is provisioned automatically via Bicep in `infrastructure/main.bicep`:

```bicep
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-${resourceNamePrefix}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    SamplingPercentage: 100
    RetentionInDays: 90
  }
}
```

The connection string is automatically injected into container apps as an environment variable.

## Features

### 1. Automatic Correlation IDs

Application Insights automatically generates and propagates correlation IDs (Operation IDs) across:
- HTTP requests between services
- External API calls
- Database queries
- Background operations

**How it works:**
- Each incoming request gets a unique `Operation-Id`
- Outgoing dependencies inherit the same `Operation-Id`
- Parent-child relationships tracked via `Operation-ParentId`

**Viewing correlation:**
```kql
// Kusto Query to trace a full request
requests
| where timestamp > ago(1h)
| where name contains "GetSubmission"
| project operation_Id, name, duration, timestamp
| join kind=inner (
    dependencies
    | where timestamp > ago(1h)
) on operation_Id
| project timestamp, operation_Id, requestName=name, dependencyName=name1, duration, duration1
| order by timestamp desc
```

### 2. Custom Telemetry Processor

The `CustomTelemetryProcessor` enhances all telemetry with additional context:

**Request Telemetry:**
```csharp
Properties["Service"] = "MCP-Server"
Properties["Version"] = "1.0"
Properties["Component"] = "AmLink-Submission-MCP"
```

**Dependency Telemetry (AmLink APIs):**
```csharp
Properties["ApiType"] = "AmLink"
Properties["ServiceFamily"] = "AmWins"
Properties["ApiName"] = "Submission"
Properties["BusinessDomain"] = "Submissions"
```

**Dependency Telemetry (Identity Server):**
```csharp
Properties["ApiType"] = "IdentityServer"
Properties["ServiceFamily"] = "Auth"
```

### 3. Dependency Tracking

All HTTP calls are automatically tracked as dependencies:

**Tracked automatically:**
- HttpClient calls to AmLink Submission API
- OAuth token requests to Identity Server
- Health check endpoints
- Any other HTTP/HTTPS outbound calls

**Dependency data captured:**
- Target server/endpoint
- HTTP method and URL
- Duration
- Success/failure status
- Response codes
- Correlation headers

### 4. Performance Monitoring

**Metrics tracked:**
- Request duration and throughput
- Dependency call duration
- Failed request rate
- Exception rate
- Server response time

**Live Metrics Stream:**
Real-time monitoring enabled via `EnableQuickPulseMetricStream: true`

### 5. Exception Tracking

All unhandled exceptions are automatically captured with:
- Full stack trace
- Request context
- Custom properties
- Correlation information
- Timestamp and severity

## Querying Telemetry

### Common Queries

**Find slow requests:**
```kql
requests
| where timestamp > ago(1h)
| where duration > 1000  // Requests over 1 second
| project timestamp, name, duration, resultCode, operation_Id
| order by duration desc
```

**Track AmLink API calls:**
```kql
dependencies
| where timestamp > ago(1h)
| where customDimensions.ApiType == "AmLink"
| project timestamp, name, target, duration, success, customDimensions.ApiName
| order by timestamp desc
```

**Find failed requests with dependencies:**
```kql
requests
| where timestamp > ago(1h)
| where success == false
| join kind=inner (
    dependencies
    | where timestamp > ago(1h)
) on operation_Id
| project timestamp, requestName=name, dependencyName=name1, resultCode, dependencySuccess=success1
```

**Exception analysis:**
```kql
exceptions
| where timestamp > ago(24h)
| summarize count() by type, outerMessage
| order by count_ desc
```

**Custom properties filtering:**
```kql
requests
| where timestamp > ago(1h)
| where customDimensions.Service == "MCP-Server"
| where customDimensions.Component == "AmLink-Submission-MCP"
| project timestamp, name, duration, customDimensions
```

## Monitoring Dashboard

### Recommended Alerts

1. **High Error Rate:**
   - Condition: Request failure rate > 5%
   - Severity: High

2. **Slow Dependency Calls:**
   - Condition: Avg dependency duration > 2 seconds
   - Severity: Medium

3. **Exception Threshold:**
   - Condition: Exception count > 10 in 5 minutes
   - Severity: High

### Key Metrics to Monitor

- **Availability:** Server response time and uptime
- **Performance:** Request duration percentiles (p50, p95, p99)
- **Dependencies:** External API call success rate and duration
- **Errors:** Exception rate and types
- **Usage:** Request volume and patterns

## Best Practices

### 1. Correlation Context

Always use structured logging with correlation:
```csharp
_logger.LogInformation(
    "Processing submission {SubmissionId} for operation {OperationId}",
    submissionId,
    Activity.Current?.Id);
```

### 2. Custom Events

Track business-specific events:
```csharp
telemetryClient.TrackEvent("SubmissionProcessed", new Dictionary<string, string>
{
    ["SubmissionId"] = submissionId.ToString(),
    ["Status"] = "Success"
});
```

### 3. Custom Metrics

Track business metrics:
```csharp
telemetryClient.TrackMetric("SubmissionQueueLength", queueLength);
```

### 4. Sampling Strategy

- **Development:** 100% sampling for full visibility
- **Staging:** 50% sampling for cost optimization
- **Production:** Adaptive sampling (automatically adjusts)

### 5. Data Retention

- Application Insights: 90 days (configured)
- Log Analytics: 30 days (default)
- Archive to Storage Account for long-term retention if needed

## Troubleshooting

### No Telemetry Appearing

**Check:**
1. Connection string is set correctly
2. Application Insights is provisioned in Azure
3. Network connectivity to ingestion endpoint
4. Firewall rules allow outbound HTTPS

**Verify configuration:**
```bash
# In container logs
grep -i "applicationinsights" /app/logs/*

# Check environment variable
echo $ConnectionStrings__ApplicationInsights
```

### Correlation Not Working

**Ensure:**
1. Both services use Application Insights SDK
2. HTTP clients are registered via DI (for automatic instrumentation)
3. Correlation headers are not stripped by proxies/load balancers

### Missing Custom Properties

**Verify:**
1. CustomTelemetryProcessor is registered
2. Processor logic executes (add logging)
3. Properties are added before calling `_next.Process(item)`

## Development Testing

### Local Development

For local testing without Azure:
```json
{
  "ApplicationInsights": {
    "ConnectionString": "",  // Leave empty - telemetry won't be sent
    "EnableAdaptiveSampling": false
  }
}
```

### Testing Correlation

```csharp
// Example test
[Fact]
public async Task RequestFlow_Should_PreserveCorrelationId()
{
    var operationId = Guid.NewGuid().ToString();
    
    // Start activity with operation ID
    using var activity = new Activity("TestOperation");
    activity.SetIdFormat(ActivityIdFormat.W3C);
    activity.Start();
    
    // Make request - correlation should propagate
    var response = await _httpClient.GetAsync("/api/submissions/123");
    
    // Verify operation ID is in telemetry
    Assert.Equal(operationId, activity.Id);
}
```

## Cost Optimization

### Sampling Strategies

**Adaptive Sampling (Recommended):**
- Automatically adjusts sampling based on traffic
- Maintains statistical accuracy
- Reduces costs during high traffic

**Fixed Sampling:**
```json
{
  "ApplicationInsights": {
    "EnableAdaptiveSampling": false,
    "SamplingPercentage": 50
  }
}
```

### Data Volume Control

1. **Exclude health checks:** Filter out frequent health check requests
2. **Reduce retention:** Lower retention days if not needed
3. **Archive old data:** Move to cheaper storage
4. **Use Log Analytics:** Query across multiple sources efficiently

## References

- [Application Insights Overview](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)
- [ASP.NET Core Application Insights](https://docs.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core)
- [Correlation in Application Insights](https://docs.microsoft.com/en-us/azure/azure-monitor/app/correlation)
- [Kusto Query Language](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/query/)
- [Application Insights API](https://docs.microsoft.com/en-us/azure/azure-monitor/app/api-custom-events-metrics)

## Support

For issues or questions:
- Check Application Insights logs in Azure Portal
- Review this documentation
- Contact the DevOps team for Azure access
- See [TROUBLESHOOTING.md](./TROUBLESHOOTING.md) for common issues
