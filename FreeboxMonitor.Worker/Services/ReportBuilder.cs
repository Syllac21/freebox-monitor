namespace FreeboxMonitor.Worker.Services;

public class ReportBuilder
{
    public string BuildDailyReportHtml(List<(string DeviceName, bool IsReachable, DateTimeOffset EventTime)> events)
    {
        if (events.Count == 0)
        {
            return "<p>Aucun événement enregistré aujourd'hui.</p>";
        }

        var groupedByDevice = events
            .GroupBy(e => e.DeviceName)
            .OrderBy(g => g.Key);

        var sections = groupedByDevice.Select(group =>
        {
            var rows = string.Join("", group.Select(e =>
                $"<tr><td>{e.EventTime.ToLocalTime():HH:mm}</td><td>{(e.IsReachable ? "Connexion" : "Déconnexion")}</td></tr>"));

            return $"""
                <h2>{group.Key}</h2>
                <table border="1" cellpadding="5">
                    <tr><th>Heure</th><th>Événement</th></tr>
                    {rows}
                </table>
                """;
        });

        return string.Join("<br>", sections);
    }
}
