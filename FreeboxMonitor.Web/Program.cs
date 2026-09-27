using FreeboxMonitor.Web.Middleware;
using FreeboxMonitor.Web.Models;
using FreeboxMonitor.Web.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<FreeboxOptions>(
    builder.Configuration.GetSection(FreeboxOptions.SectionName));

builder.Services.AddHttpClient<FreeboxAuthService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Freebox:BaseUrl"]!);
});

builder.Services.AddHttpClient<FreeboxLanService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Freebox:BaseUrl"]!);
});

builder.Services.AddSingleton<DeviceEventRepository>();

var app = builder.Build();

app.UseMiddleware<BasicAuthMiddleware>();

app.MapGet("/", async (DeviceEventRepository repository) =>
{
    var currentStatus = await repository.GetCurrentStatusAsync();
    var todayEvents = await repository.GetTodayEventsAsync();

    return Results.Text(BuildHtml(currentStatus, todayEvents), "text/html");
});

app.MapPost("/refresh", async (
    FreeboxAuthService freeboxAuth,
    FreeboxLanService freeboxLan,
    DeviceEventRepository repository,
    IOptions<FreeboxOptions> freeboxOptions) =>
{
    var options = freeboxOptions.Value;

    var session = await freeboxAuth.OpenSessionAsync(options.AppId, options.AppToken);
    if (session is null)
    {
        return Results.Redirect("/");
    }

    var hosts = await freeboxLan.GetHostsAsync(session.SessionToken);
    if (hosts is null)
    {
        return Results.Redirect("/");
    }

    var trackedHosts = hosts.Where(h => options.TrackedDeviceNames.Contains(h.PrimaryName));

    foreach (var host in trackedHosts)
    {
        await repository.InsertEventIfChangedAsync(host.PrimaryName, host.Reachable);
    }

    return Results.Redirect("/");
});

app.Run();

static string BuildHtml(List<FreeboxMonitor.Web.Models.DeviceStatus> currentStatus, List<FreeboxMonitor.Web.Models.DeviceStatus> todayEvents)
{
    const string StyleBlock = """
        <style>
            body {
                font-family: -apple-system, sans-serif;
                font-size: 16px;
                padding: 16px;
                margin: 0;
            }
            h1 {
                font-size: 20px;
            }
            table {
                width: 100%;
                border-collapse: collapse;
                margin-bottom: 24px;
            }
            th, td {
                padding: 10px 8px;
                text-align: left;
                border-bottom: 1px solid #ddd;
                font-size: 15px;
            }
            th {
                background: #f2f2f2;
            }
            button {
                font-size: 16px;
                padding: 10px 16px;
                margin-bottom: 16px;
                border: none;
                border-radius: 6px;
                background: #007aff;
                color: white;
            }
        </style>
        """;

    var connectedDevices = currentStatus.Where(d => d.IsReachable).ToList();

    var connectedRows = connectedDevices.Count > 0
        ? string.Join("", connectedDevices.Select(d => $"<tr><td>{d.DeviceName}</td></tr>"))
        : "<tr><td>Aucun appareil connecté actuellement</td></tr>";

    var currentRows = string.Join("", currentStatus.Select(d =>
        $"<tr><td>{d.DeviceName}</td><td>{(d.IsReachable ? "🟢 Connecté" : "🔴 Déconnecté")}</td></tr>"));

    var eventRows = string.Join("", todayEvents.Select(e =>
        $"<tr><td>{e.EventTime.ToLocalTime():HH:mm}</td><td>{e.DeviceName}</td><td>{(e.IsReachable ? "Connexion" : "Déconnexion")}</td></tr>"));

    return $"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>Freebox Monitor</title>
            {StyleBlock}
        </head>
        <body>
            <form method="post" action="/refresh">
                <button type="submit">🔄 Actualiser</button>
            </form>

            <h1>Appareils connectés</h1>
            <table>
                <tr><th>Appareil</th></tr>
                {connectedRows}
            </table>

            <h1>État complet</h1>
            <table>
                <tr><th>Appareil</th><th>Statut</th></tr>
                {currentRows}
            </table>

            <h1>Événements du jour</h1>
            <table>
                <tr><th>Heure</th><th>Appareil</th><th>Événement</th></tr>
                {eventRows}
            </table>
        </body>
        </html>
        """;
}
