using System.Net.Http.Json;
using System.Text.Json;
using FreeboxMonitor.Worker.Models;

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


}
