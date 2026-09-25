using System.Net.Http.Json;
using System.Text.Json;
using FreeboxMonitor.Worker.Models;

namespace FreeboxMonitor.Worker.Services;

public class FreeboxLanService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FreeboxLanService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public FreeboxLanService(HttpClient httpClient, ILogger<FreeboxLanService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<LanHost>?> GetHostsAsync(string sessionToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v8/lan/browser/pub/");
        request.Headers.Add("X-Fbx-App-Auth", sessionToken);

        var response = await _httpClient.SendAsync(request);

        var content = await response.Content.ReadFromJsonAsync<FreeboxResponse<List<LanHost>>>(_jsonOptions);

        if (content is null || !content.Success)
        {
            _logger.LogError("Échec de la récupération des appareils : {Msg}", content?.Msg);
            return null;
        }

        return content.Result;
    }
}
