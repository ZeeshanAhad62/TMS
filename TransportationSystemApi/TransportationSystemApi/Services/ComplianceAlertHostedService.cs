namespace TransportationSystemApi.Services;

// Runs ComplianceAlertService.RunScanAsync on a timer (default: once every
// 24h, first run right after startup). Interval is configurable so it can be
// shortened for local testing without waiting a full day.
public class ComplianceAlertHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<ComplianceAlertHostedService> _logger;

    public ComplianceAlertHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<ComplianceAlertHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = _config.GetValue<double?>("Compliance:AlertScanIntervalHours") ?? 24;
        using var timer = new PeriodicTimer(TimeSpan.FromHours(intervalHours));

        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var alertService = scope.ServiceProvider.GetRequiredService<ComplianceAlertService>();
                await alertService.RunScanAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Compliance alert scan failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
