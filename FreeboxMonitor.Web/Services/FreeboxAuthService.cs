using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FreeboxMonitor.Web.Models;

namespace FreeboxMonitor.Web.Services;

public class FreeboxAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FreeboxAuthService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public FreeboxAuthService(HttpClient httpClient, ILogger<FreeboxAuthService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<LoginStatus?> GetChallengeAsync()
    {
        var response = await _httpClient.GetAsync("/api/v8/login/");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<FreeboxResponse<LoginStatus>>(_jsonOptions);

        if (content is null || !content.Success)
        {
            _logger.LogError("Échec de la récupération du challenge : {Msg}", content?.Msg);
            return null;
        }

        return content.Result;
    }

    private string ComputePassword(string challenge, string appToken)
    {
        var keyBytes = Encoding.UTF8.GetBytes(appToken);
        var challengeBytes = Encoding.UTF8.GetBytes(challenge);

        using var hmac = new HMACSHA1(keyBytes);
        var hashBytes = hmac.ComputeHash(challengeBytes);

        return Convert.ToHexString(hashBytes).ToLower();
    }

    public async Task<OpenSessionResult?> OpenSessionAsync(string appId, string appToken)
    {
        var challengeResult = await GetChallengeAsync();
        if (challengeResult is null)
        {
            return null;
        }

        var password = ComputePassword(challengeResult.Challenge, appToken);

        var requestBody = new
        {
            app_id = appId,
            password
        };

        var response = await _httpClient.PostAsJsonAsync("/api/v8/login/session/", requestBody);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<FreeboxResponse<OpenSessionResult>>(_jsonOptions);

        if (content is null || !content.Success)
        {
            _logger.LogError("Échec de l'ouverture de session : {Msg}", content?.Msg);
            return null;
        }

        return content.Result;
    }
}
