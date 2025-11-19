using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AmLink.Submissions.Mcp.Client.Configuration;

namespace AmLink.Submissions.Mcp.Client.Services;

public interface ITokenService
{
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    Task<string> InitiateAuthenticationAsync(CancellationToken cancellationToken = default);
    Task<bool> CompleteAuthenticationAsync(string authorizationCode, string state, CancellationToken cancellationToken = default);
    bool IsAuthenticated { get; }
    void ClearToken();
}

public class TokenService : ITokenService
{
    private readonly IdentityServerConfiguration _idsConfig;
    private readonly HttpClient _httpClient;
    private readonly ILogger<TokenService> _logger;
    
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private readonly ConcurrentDictionary<string, string> _codeVerifiers = new();

    public TokenService(
        IdentityServerConfiguration idsConfig,
        HttpClient httpClient,
        ILogger<TokenService> logger)
    {
        _idsConfig = idsConfig;
        _httpClient = httpClient;
        _logger = logger;
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken) && _tokenExpiry > DateTime.UtcNow.AddMinutes(5);

    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(IsAuthenticated ? _accessToken : null);
    }

    public async Task<string> InitiateAuthenticationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var state = Guid.NewGuid().ToString();
            var codeVerifier = GenerateCodeVerifier();
            var codeChallenge = GenerateCodeChallenge(codeVerifier);
            
            // Store code verifier for later use
            _codeVerifiers[state] = codeVerifier;
            
            var parameters = new Dictionary<string, string>
            {
                ["client_id"] = _idsConfig.ClientId,
                ["response_type"] = "code",
                ["redirect_uri"] = _idsConfig.RedirectUri,
                ["scope"] = string.Join(" ", _idsConfig.ScopesList),
                ["state"] = state,
                ["code_challenge"] = codeChallenge,
                ["code_challenge_method"] = "S256"
            };

            var queryString = string.Join("&", 
                parameters.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
            
            var authUrl = $"{_idsConfig.Url}/connect/authorize?{queryString}";
            
            _logger.LogInformation("Generated authorization URL for state: {State}", state);
            return authUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate authentication");
            throw;
        }
    }

    public async Task<bool> CompleteAuthenticationAsync(string authorizationCode, string state, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_codeVerifiers.TryRemove(state, out var codeVerifier))
            {
                _logger.LogWarning("Code verifier not found for state: {State}", state);
                return false;
            }

            var tokenResponse = await ExchangeCodeForTokenAsync(authorizationCode, codeVerifier, cancellationToken);
            
            _accessToken = tokenResponse.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
            
            _logger.LogInformation("Authentication completed successfully. Token expires at: {Expiry}", _tokenExpiry);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete authentication");
            return false;
        }
    }

    public void ClearToken()
    {
        _accessToken = null;
        _tokenExpiry = DateTime.MinValue;
        _codeVerifiers.Clear();
        _logger.LogInformation("Token cleared");
    }

    private async Task<TokenResponse> ExchangeCodeForTokenAsync(string authorizationCode, string codeVerifier, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _idsConfig.ClientId,
            ["client_secret"] = _idsConfig.ClientSecret,
            ["code"] = authorizationCode,
            ["redirect_uri"] = _idsConfig.RedirectUri,
            ["code_verifier"] = codeVerifier
        };

        var content = new FormUrlEncodedContent(parameters);
        var response = await _httpClient.PostAsync($"{_idsConfig.Url}/connect/token", content, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Token exchange failed: {StatusCode} - {Error}", response.StatusCode, error);
            throw new InvalidOperationException($"Token exchange failed: {response.StatusCode} - {error}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower 
        });

        return tokenResponse ?? throw new InvalidOperationException("Invalid token response");
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        return Convert.ToBase64String(hash)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string? RefreshToken { get; set; }
        public string? Scope { get; set; }
    }
}