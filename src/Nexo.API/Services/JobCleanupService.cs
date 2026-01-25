using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Nexo.API.Services;

/// <summary>
/// Background service that periodically cleans up old jobs.
/// </summary>
public class JobCleanupService : BackgroundService
{
    private readonly IJobRepository _jobRepository;
    private readonly ILogger<JobCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);
    private readonly TimeSpan _jobRetentionPeriod = TimeSpan.FromDays(7);

    public JobCleanupService(IJobRepository jobRepository, ILogger<JobCleanupService> logger)
    {
        _jobRepository = jobRepository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _jobRepository.DeleteOldJobsAsync(_jobRetentionPeriod, stoppingToken);
                _logger.LogInformation("Job cleanup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during job cleanup");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }
    }
}
