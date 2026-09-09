using System.Net.Http.Json;
using System.Text.Json;
using FreeboxMonitor.Worker.Models;
using System.Security.Cryptography;
using System.Text;

namespace FreeboxMonitor.Worker.Services;

public class FreeboxAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FreeboxAuthService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public FreeboxAuthService(HttpClient httpclient, ILogger<FreeboxAuthService> logger)
    {
        _httpClient = httpclient;
        _logger = logger;
    }

    public async Task<AuthorizeResult?> RequestAuthorizationAsync(
        string appId,
        string appName,
        string appVersion,
        string deviceName
    )
    {
        var requestBody = new
        {
            app_id = appId,
            app_name = appName,
            app_version = appVersion,
            device_name = deviceName
        };

        var response = await _httpClient.PostAsJsonAsync("/api/v8/login/authorize/", requestBody);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<FreeboxResponse<AuthorizeResult>>(_jsonOptions);

        if (content is null || !content.Success)
        {
            _logger.LogError("Echec de la demande d'autorisation : {Msg}", content?.Msg);
            return null;
        }

        _logger.LogInformation("Demande envoyée. Doit être validée sur l'écran de la Freebox");
        return content.Result;
    }

    public async Task<AuthorizeStatus?> CheckAuthorizationStatusAsync(int trackId)
    {
        var response = await _httpClient.GetAsync($"/api/v8/login/authorize/{trackId}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<FreeboxResponse<AuthorizeStatus>>(_jsonOptions);
        if (content is null || !content.Success)
        {
            _logger.LogError("Echec de la vérification du statut : {Msg}", content?.Msg);
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

        _logger.LogInformation("Session ouverte avec succès");
        return content.Result;
    }

}
