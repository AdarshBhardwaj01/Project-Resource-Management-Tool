using PRM.Business.Interfaces.Repositories;
using PRM.Business.Interfaces.Services;
using PRM.Common.Constants;

namespace PRM.Api.BackgroundServices;

public class PrmBackgroundSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PrmBackgroundSchedulerService> _logger;

    public PrmBackgroundSchedulerService(
        IServiceProvider serviceProvider,
        ILogger<PrmBackgroundSchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PRM background scheduler started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunSchedulerCycleAsync(stoppingToken);

            var intervalHours = await GetSchedulerIntervalHoursAsync(stoppingToken);

            _logger.LogInformation(
                "PRM background scheduler sleeping for {IntervalHours} hour(s).",
                intervalHours);

            try
            {
                await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("PRM background scheduler stopped.");
    }

    private async Task RunSchedulerCycleAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var schedulerService = scope.ServiceProvider.GetRequiredService<IPrmSchedulerService>();

            _logger.LogInformation("PRM background scheduler cycle started.");
            await schedulerService.RunScheduledTasksAsync(stoppingToken);
            _logger.LogInformation("PRM background scheduler cycle completed.");
        }
        catch (Exception ex) when (ex is not TaskCanceledException)
        {
            _logger.LogError(ex, "PRM background scheduler cycle failed.");
        }
    }

    private async Task<int> GetSchedulerIntervalHoursAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var systemConfigRepository = scope.ServiceProvider.GetRequiredService<ISystemConfigRepository>();
        var config = await systemConfigRepository.GetSingletonAsync(stoppingToken);

        var intervalHours = config?.SchedulerIntervalHours ?? SystemDefaults.SchedulerIntervalHours;

        return intervalHours > 0
            ? intervalHours
            : SystemDefaults.SchedulerIntervalHours;
    }
}
