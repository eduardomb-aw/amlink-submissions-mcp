namespace AmLink.Submission.Mcp.Server.Configuration;

public sealed class ExternalApisConfiguration
{
    public const string SectionName = "ExternalApis";

    public SubmissionApiConfiguration SubmissionApi { get; set; } = new();
}

public sealed class SubmissionApiConfiguration
{
    public string BaseUrl { get; set; } = "https://submission-api.amwins.com";
    public string RequiredScope { get; set; } = "amlink-submission-api";
    public string UserAgent { get; set; } = "mcp-submission-client";
    public string Version { get; set; } = "1.0";
}