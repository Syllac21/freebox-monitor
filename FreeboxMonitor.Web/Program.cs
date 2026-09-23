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
            </style>
            """;

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
