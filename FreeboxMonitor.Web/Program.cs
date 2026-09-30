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
builder.Services.AddSingleton<TrackedDeviceRepository>();

var app = builder.Build();

app.UseMiddleware<BasicAuthMiddleware>();

app.MapGet("/", async (DeviceEventRepository repository) =>
{
    var currentStatus = await repository.GetCurrentStatusAsync();
    var todayEvents = await repository.GetTodayEventsAsync();

    return Results.Text(BuildHtml(currentStatus, todayEvents), "text/html");
});
app.MapGet("/devices", async (
    FreeboxAuthService freeboxAuth,
    FreeboxLanService freeboxLan,
    TrackedDeviceRepository trackedDeviceRepository,
    IOptions<FreeboxOptions> freeboxOptions) =>
{
    var options = freeboxOptions.Value;
    var trackedNames = await trackedDeviceRepository.GetTrackedDeviceNamesAsync();

    var session = await freeboxAuth.OpenSessionAsync(options.AppId, options.AppToken);
    var allHosts = session is not null
        ? await freeboxLan.GetHostsAsync(session.SessionToken) ?? []
        : [];

    return Results.Text(BuildDevicesHtml(trackedNames, allHosts), "text/html");
});

app.MapPost("/devices/add", async (HttpRequest request, TrackedDeviceRepository repository) =>
{
    var form = await request.ReadFormAsync();
    var deviceName = form["deviceName"].ToString();

    if (!string.IsNullOrWhiteSpace(deviceName))
    {
        await repository.AddTrackedDeviceAsync(deviceName);
    }

    return Results.Redirect("/devices");
});

app.MapPost("/devices/remove", async (HttpRequest request, TrackedDeviceRepository repository) =>
{
    var form = await request.ReadFormAsync();
    var deviceName = form["deviceName"].ToString();

    if (!string.IsNullOrWhiteSpace(deviceName))
    {
        await repository.RemoveTrackedDeviceAsync(deviceName);
    }

    return Results.Redirect("/devices");
});

app.MapPost("/refresh", async (
    FreeboxAuthService freeboxAuth,
    FreeboxLanService freeboxLan,
    DeviceEventRepository repository,
    TrackedDeviceRepository trackedDeviceRepository,
    IOptions<FreeboxOptions> freeboxOptions) =>
{
    var options = freeboxOptions.Value;
    var trackedNames = await trackedDeviceRepository.GetTrackedDeviceNamesAsync();

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

    var trackedHosts = hosts.Where(h => trackedNames.Contains(h.PrimaryName));

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
            <a href="/devices">⚙️ Gérer les appareils</a>
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

static string BuildDevicesHtml(List<string> trackedNames, List<FreeboxMonitor.Web.Models.LanHost> allHosts)
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
                font-size: 14px;
                padding: 6px 12px;
                border: none;
                border-radius: 6px;
                color: white;
            }
            .btn-remove {
                background: #ff3b30;
            }
            .btn-add {
                background: #34c759;
            }
            a {
                display: inline-block;
                margin-bottom: 16px;
            }
        </style>
        """;

    var trackedRows = trackedNames.Count > 0
        ? string.Join("", trackedNames.Select(name => $"""
            <tr>
                <td>{name}</td>
                <td>
                    <form method="post" action="/devices/remove" style="margin:0">
                        <input type="hidden" name="deviceName" value="{name}">
                        <button type="submit" class="btn-remove">Retirer</button>
                    </form>
                </td>
            </tr>
            """))
        : "<tr><td colspan=\"2\">Aucun appareil suivi</td></tr>";

    var untrackedHosts = allHosts
        .Where(h => !string.IsNullOrWhiteSpace(h.PrimaryName) && !trackedNames.Contains(h.PrimaryName))
        .OrderBy(h => h.PrimaryName)
        .ToList();

    var availableRows = untrackedHosts.Count > 0
        ? string.Join("", untrackedHosts.Select(h => $"""
            <tr>
                <td>{h.PrimaryName}</td>
                <td>{(h.Reachable ? "🟢 Connecté" : "🔴 Déconnecté")}</td>
                <td>
                    <form method="post" action="/devices/add" style="margin:0">
                        <input type="hidden" name="deviceName" value="{h.PrimaryName}">
                        <button type="submit" class="btn-add">Ajouter</button>
                    </form>
                </td>
            </tr>
            """))
        : "<tr><td colspan=\"3\">Aucun appareil disponible</td></tr>";

    return $"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>Gérer les appareils</title>
            {StyleBlock}
        </head>
        <body>
            <a href="/">← Retour au dashboard</a>

            <h1>Appareils suivis</h1>
            <table>
                <tr><th>Appareil</th><th></th></tr>
                {trackedRows}
            </table>

            <h1>Appareils disponibles</h1>
            <table>
                <tr><th>Appareil</th><th>Statut</th><th></th></tr>
                {availableRows}
            </table>
        </body>
        </html>
        """;
}
