using FreeboxMonitor.Worker.Services;
using FreeboxMonitor.Worker.Models;

namespace FreeboxMonitor.Worker;

public class Worker(
    ILogger<Worker> logger,
    FreeboxAuthService freeboxAuth,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var result = await freeboxAuth.RequestAuthorizationAsync(
            appId: configuration["Freebox:AppId"]!,
            appName: configuration["Freebox:AppName"]!,
            appVersion: configuration["Freebox:AppVersion"]!,
            deviceName: configuration["Freebox:DeviceName"]!);

        if (result is null)
        {
            logger.LogError("Échec de la demande d'autorisation, arrêt.");
            return;
        }

        logger.LogInformation("app_token reçu : {Token}", result.AppToken);
        logger.LogInformation("En attente de validation sur l'écran de la Freebox...");

        AuthorizeStatus? status;
        do
        {
            await Task.Delay(2000, stoppingToken);
            status = await freeboxAuth.CheckAuthorizationStatusAsync(result.TrackId);
            logger.LogInformation("Statut actuel : {Status}", status?.Status);
        }
        while (status is not null && status.Status == "pending");

        if (status?.Status == "granted")
        {
            logger.LogInformation("Autorisation validée !");
        }
        else
        {
            logger.LogError("Autorisation refusée ou expirée : {Status}", status?.Status);
        }
    }
}
