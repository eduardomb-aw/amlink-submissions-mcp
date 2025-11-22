using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace AmLink.Submission.Mcp.Server.Telemetry;

/// <summary>
/// Custom telemetry processor to enhance telemetry with additional context
/// </summary>
public class CustomTelemetryProcessor : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next;

    public CustomTelemetryProcessor(ITelemetryProcessor next)
    {
        _next = next;
    }

    public void Process(ITelemetry item)
    {
        // Enhance request telemetry
        if (item is RequestTelemetry request)
        {
            // Add custom properties for better filtering and analysis
            request.Properties["Service"] = "MCP-Server";
            request.Properties["Version"] = "1.0";
            request.Properties["Component"] = "AmLink-Submission-MCP";
        }

        // Enhance dependency telemetry (external API calls)
        if (item is DependencyTelemetry dependency)
        {
            // Identify and tag AmLink API calls
            if (dependency.Target?.Contains("amwins.net") == true)
            {
                dependency.Properties["ApiType"] = "AmLink";
                dependency.Properties["ServiceFamily"] = "AmWins";
            }

            // Tag submission API calls specifically
            if (dependency.Target?.Contains("amlink-submission-api") == true)
            {
                dependency.Properties["ApiName"] = "Submission";
                dependency.Properties["BusinessDomain"] = "Submissions";
            }

            // Tag identity server calls (supports identity*.amwins.com and identity*.amwins.net)
            if ((dependency.Target?.StartsWith("identity", StringComparison.OrdinalIgnoreCase) == true &&
                 (dependency.Target.Contains(".amwins.com") || dependency.Target.Contains(".amwins.net"))))
            {
                dependency.Properties["ApiType"] = "IdentityServer";
                dependency.Properties["ServiceFamily"] = "Auth";
            }
        }

        // Enhance exception telemetry
        if (item is ExceptionTelemetry exception)
        {
            exception.Properties["Service"] = "MCP-Server";
            exception.Properties["Component"] = "AmLink-Submission-MCP";
        }

        // Enhance trace telemetry
        if (item is TraceTelemetry trace)
        {
            trace.Properties["Service"] = "MCP-Server";
        }

        // Continue the telemetry pipeline
        _next.Process(item);
    }
}
