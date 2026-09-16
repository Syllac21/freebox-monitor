using FreeboxMonitor.Web.Middleware;
using FreeboxMonitor.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DeviceEventRepository>();

var app = builder.Build();

app.UseMiddleware<BasicAuthMiddleware>();

app.MapGet("/", async (DeviceEventRepository repository) =>
{
    var currentStatus = await repository.GetCurrentStatusAsync();
    var todayEvents = await repository.GetTodayEventsAsync();

    return Results.Text(BuildHtml(currentStatus, todayEvents), "text/html");
});

app.Run();

static string BuildHtml(List<FreeboxMonitor.Web.Models.DeviceStatus> currentStatus, List<FreeboxMonitor.Web.Models.DeviceStatus> todayEvents)
{
    var currentRows = string.Join("", currentStatus.Select(d =>
        $"<tr><td>{d.DeviceName}</td><td>{(d.IsReachable ? "🟢 Connecté" : "🔴 Déconnecté")}</td></tr>"));

    var eventRows = string.Join("", todayEvents.Select(e =>
        $"<tr><td>{e.EventTime.ToLocalTime():HH:mm}</td><td>{e.DeviceName}</td><td>{(e.IsReachable ? "Connexion" : "Déconnexion")}</td></tr>"));

    return $"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <title>Freebox Monitor</title>
        </head>
        <body>
            <h1>État actuel</h1>
            <table border="1">
                <tr><th>Appareil</th><th>Statut</th></tr>
                {currentRows}
            </table>

            <h1>Événements du jour</h1>
            <table border="1">
                <tr><th>Heure</th><th>Appareil</th><th>Événement</th></tr>
                {eventRows}
            </table>
        </body>
        </html>
        """;
}
