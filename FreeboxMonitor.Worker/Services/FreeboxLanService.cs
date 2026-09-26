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
        var json = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var success = root.TryGetProperty("success", out var successProp) && successProp.GetBoolean();

        if (!success)
        {
            var msg = root.TryGetProperty("msg", out var msgProp) ? msgProp.GetString() : "raison inconnue";
            _logger.LogError("Échec de la récupération des appareils : {Msg}", msg);
            return null;
        }

        if (!root.TryGetProperty("result", out var resultProp))
        {
            return null;
        }

        return JsonSerializer.Deserialize<List<LanHost>>(resultProp.GetRawText(), _jsonOptions);
    }
}
