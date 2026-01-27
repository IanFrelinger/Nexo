using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nexo.API.Services;

/// <summary>
/// Configuration for job cleanup service.
/// </summary>
public class JobCleanupOptions
{
    /// <summary>
    /// How often to run cleanup (default: 1 hour).
    /// </summary>
    public int CleanupIntervalHours { get; set; } = 1;

    /// <summary>
    /// How long to retain jobs before deletion (default: 7 days).
    /// </summary>
    public int JobRetentionDays { get; set; } = 7;
}

/// <summary>
/// Background service that periodically cleans up old jobs.
/// </summary>
public class JobCleanupService : BackgroundService
{
    private readonly IJobRepository _jobRepository;
    private readonly ILogger<JobCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval;
    private readonly TimeSpan _jobRetentionPeriod;

    public JobCleanupService(
        IJobRepository jobRepository, 
        ILogger<JobCleanupService> logger,
        IOptions<JobCleanupOptions>? options = null)
    {
        _jobRepository = jobRepository;
        _logger = logger;
        
        var opts = options?.Value ?? new JobCleanupOptions();
        _cleanupInterval = TimeSpan.FromHours(opts.CleanupIntervalHours);
        _jobRetentionPeriod = TimeSpan.FromDays(opts.JobRetentionDays);
        
        _logger.LogInformation(
            "Job cleanup service configured: cleanup interval={Interval}, retention period={Retention}",
            _cleanupInterval, _jobRetentionPeriod);
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
