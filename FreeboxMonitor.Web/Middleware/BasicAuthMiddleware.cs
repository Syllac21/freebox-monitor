using System.Text;

namespace FreeboxMonitor.Web.Middleware;

public class BasicAuthMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var expectedUsername = configuration["DashboardAuth:Username"];
        var expectedPassword = configuration["DashboardAuth:Password"];

        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader) ||
            !TryValidateCredentials(authHeader!, expectedUsername, expectedPassword))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"Freebox Monitor\"");
            return;
        }

        await next(context);
    }

    private static bool TryValidateCredentials(string authHeader, string? expectedUsername, string? expectedPassword)
    {
        if (!authHeader.StartsWith("Basic "))
        {
            return false;
        }

        var encodedCredentials = authHeader["Basic ".Length..];
        var decodedBytes = Convert.FromBase64String(encodedCredentials);
        var decodedCredentials = Encoding.UTF8.GetString(decodedBytes);

        var parts = decodedCredentials.Split(':', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        var (username, password) = (parts[0], parts[1]);
        return username == expectedUsername && password == expectedPassword;
    }
}
