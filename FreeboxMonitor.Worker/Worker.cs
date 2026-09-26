using FreeboxMonitor.Worker.Models;
using FreeboxMonitor.Worker.Services;
using Microsoft.Extensions.Options;

namespace FreeboxMonitor.Worker;

public class Worker(
    ILogger<Worker> logger,
    FreeboxAuthService freeboxAuth,
    FreeboxLanService freeboxLan,
    DeviceEventRepository deviceEventRepository,
    ReportBuilder reportBuilder,
    EmailService emailService,
    IOptions<FreeboxOptions> freeboxOptions) : BackgroundService
{
    private readonly FreeboxOptions _options = freeboxOptions.Value;
    private readonly Dictionary<string, bool> _lastKnownState = new();
    private string? _sessionToken;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrEmpty(_options.AppToken))
        {
            logger.LogError("Aucun app_token trouvé dans la configuration.");
            return;
        }

        var pollingTask = RunPollingLoopAsync(stoppingToken);
        var reportingTask = RunDailyReportLoopAsync(stoppingToken);

        await Task.WhenAll(pollingTask, reportingTask);
    }

    private async Task RunPollingLoopAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(2));

        do
        {
            try
            {
                await PollAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erreur pendant le polling, on continue à la prochaine itération");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunDailyReportLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextReportTime = now.Date.AddHours(20);

            if (now >= nextReportTime)
            {
                nextReportTime = nextReportTime.AddDays(1);
            }

            var delay = nextReportTime - now;
            logger.LogInformation("Prochain rapport quotidien prévu à {Time}", nextReportTime);

            await Task.Delay(delay, stoppingToken);

            await SendDailyReportAsync();
        }
    }

    private async Task SendDailyReportAsync()
    {
        try
        {
            var events = await deviceEventRepository.GetTodayEventsAsync();
            var html = reportBuilder.BuildDailyReportHtml(events);

            await emailService.SendReportAsync(
                $"Rapport Freebox Monitor — {DateTime.Now:dd/MM/yyyy}",
                html);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Échec de la génération/envoi du rapport quotidien");
        }
    }

    private async Task<bool> EnsureSessionAsync()
    {
        if (_sessionToken is not null)
        {
            return true;
        }

        var session = await freeboxAuth.OpenSessionAsync(_options.AppId, _options.AppToken!);

        if (session is null)
        {
            logger.LogError("Impossible d'ouvrir une session.");
            return false;
        }

        _sessionToken = session.SessionToken;
        return true;
    }

    private async Task PollAsync()
    {
        if (!await EnsureSessionAsync())
        {
            return;
        }

        var hosts = await freeboxLan.GetHostsAsync(_sessionToken!);

        if (hosts is null)
        {
            logger.LogWarning("Échec de récupération des appareils, la session a peut-être expiré. Nouvelle tentative...");
            _sessionToken = null;

            if (!await EnsureSessionAsync())
            {
                return;
            }

            hosts = await freeboxLan.GetHostsAsync(_sessionToken!);

            if (hosts is null)
            {
                logger.LogError("Échec définitif de récupération des appareils.");
                return;
            }
        }

        var trackedHosts = hosts
            .Where(h => _options.TrackedDeviceNames.Contains(h.PrimaryName))
            .ToList();

        foreach (var host in trackedHosts)
        {
            var hasPreviousState = _lastKnownState.TryGetValue(host.PrimaryName, out var wasReachable);

            if (!hasPreviousState || wasReachable != host.Reachable)
            {
                await deviceEventRepository.InsertEventAsync(host.PrimaryName, host.Reachable);
            }

            _lastKnownState[host.PrimaryName] = host.Reachable;
        }
    }
}
