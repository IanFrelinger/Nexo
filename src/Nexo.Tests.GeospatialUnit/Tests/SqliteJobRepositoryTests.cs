using FluentAssertions;
using Microsoft.Extensions.Logging;
using Nexo.API.Models;
using Nexo.API.Services;
using Xunit;

namespace Nexo.Tests.GeospatialUnit.Tests;

public class SqliteJobRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly ILogger<SqliteJobRepository> _logger;
    private readonly SqliteJobRepository _repository;

    public SqliteJobRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"nexo-test-{Guid.NewGuid()}.db");
        _logger = new LoggerFactory().CreateLogger<SqliteJobRepository>();
        _repository = new SqliteJobRepository(_dbPath, _logger);
    }

    [Fact]
    public async Task CreateJobAsync_ShouldStoreJob_AndReturnJobId()
    {
        // Arrange
        var job = new JobStatusResponse
        {
            JobId = Guid.NewGuid().ToString("N"),
            Status = "pending",
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var jobId = await _repository.CreateJobAsync(job);

        // Assert
        jobId.Should().Be(job.JobId);
        var retrieved = await _repository.GetJobAsync(jobId);
        retrieved.Should().NotBeNull();
        retrieved!.JobId.Should().Be(job.JobId);
        retrieved.Status.Should().Be("pending");
    }

    [Fact]
    public async Task GetJobAsync_ShouldReturnNull_WhenJobNotFound()
    {
        // Arrange
        var nonExistentJobId = Guid.NewGuid().ToString("N");

        // Act
        var result = await _repository.GetJobAsync(nonExistentJobId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateJobAsync_ShouldModifyJobStatus()
    {
        // Arrange
        var job = new JobStatusResponse
        {
            JobId = Guid.NewGuid().ToString("N"),
            Status = "pending",
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.CreateJobAsync(job);

        // Act
        var updatedJob = job with
        {
            Status = "processing",
            Progress = 50
        };
        await _repository.UpdateJobAsync(updatedJob);

        // Assert
        var retrieved = await _repository.GetJobAsync(job.JobId);
        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be("processing");
        retrieved.Progress.Should().Be(50);
    }

    [Fact]
    public async Task UpdateJobAsync_ShouldUpdateAllFields()
    {
        // Arrange
        var job = new JobStatusResponse
        {
            JobId = Guid.NewGuid().ToString("N"),
            Status = "pending",
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.CreateJobAsync(job);

        // Act
        var updatedJob = job with
        {
            Status = "completed",
            Progress = 100,
            OutputPath = "/path/to/output.obj",
            CompletedAt = DateTime.UtcNow,
            ErrorMessage = null
        };
        await _repository.UpdateJobAsync(updatedJob);

        // Assert
        var retrieved = await _repository.GetJobAsync(job.JobId);
        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be("completed");
        retrieved.Progress.Should().Be(100);
        retrieved.OutputPath.Should().Be("/path/to/output.obj");
        retrieved.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetJobsAsync_ShouldReturnJobs_OrderedByCreatedAt()
    {
        // Arrange
        var job1 = new JobStatusResponse
        {
            JobId = Guid.NewGuid().ToString("N"),
            Status = "pending",
            Progress = 0,
            CreatedAt = DateTime.UtcNow.AddMinutes(-2)
        };

        var job2 = new JobStatusResponse
        {
            JobId = Guid.NewGuid().ToString("N"),
            Status = "pending",
            Progress = 0,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        var job3 = new JobStatusResponse
        {
            JobId = Guid.NewGuid().ToString("N"),
            Status = "pending",
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.CreateJobAsync(job1);
        await _repository.CreateJobAsync(job2);
        await _repository.CreateJobAsync(job3);

        // Act
        var jobs = await _repository.GetJobsAsync(limit: 10);

        // Assert
        jobs.Should().HaveCount(3);
        // Should be ordered by created_at DESC (newest first)
        jobs[0].JobId.Should().Be(job3.JobId);
        jobs[1].JobId.Should().Be(job2.JobId);
        jobs[2].JobId.Should().Be(job1.JobId);
    }

    [Fact]
    public async Task GetJobsAsync_ShouldRespectLimit()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            var job = new JobStatusResponse
            {
                JobId = Guid.NewGuid().ToString("N"),
                Status = "pending",
                Progress = 0,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            };
            await _repository.CreateJobAsync(job);
        }

        // Act
        var jobs = await _repository.GetJobsAsync(limit: 3);

        // Assert
        jobs.Should().HaveCount(3);
    }

    [Fact]
    public async Task DeleteJobAsync_ShouldRemoveJob()
    {
        // Arrange
        var job = new JobStatusResponse
        {
            JobId = Guid.NewGuid().ToString("N"),
            Status = "pending",
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.CreateJobAsync(job);

        // Act
        await _repository.DeleteJobAsync(job.JobId);

        // Assert
        var retrieved = await _repository.GetJobAsync(job.JobId);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task DeleteOldJobsAsync_ShouldRemoveJobs_OlderThanThreshold()
    {
        // Arrange
        var oldJob = new JobStatusResponse
        {
            JobId = Guid.NewGuid().ToString("N"),
            Status = "completed",
            Progress = 100,
            CreatedAt = DateTime.UtcNow.AddDays(-8),
            CompletedAt = DateTime.UtcNow.AddDays(-8)
        };

        var recentJob = new JobStatusResponse
        {
            JobId = Guid.NewGuid().ToString("N"),
            Status = "completed",
            Progress = 100,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            CompletedAt = DateTime.UtcNow.AddDays(-1)
        };

        await _repository.CreateJobAsync(oldJob);
        await _repository.CreateJobAsync(recentJob);

        // Act
        await _repository.DeleteOldJobsAsync(TimeSpan.FromDays(7));

        // Assert
        var oldRetrieved = await _repository.GetJobAsync(oldJob.JobId);
        var recentRetrieved = await _repository.GetJobAsync(recentJob.JobId);

        oldRetrieved.Should().BeNull("Old job should be deleted");
        recentRetrieved.Should().NotBeNull("Recent job should remain");
    }

    [Fact]
    public async Task CreateJobAsync_ShouldHandleConcurrentAccess()
    {
        // Arrange
        var tasks = new List<Task<string>>();
        var jobIds = new List<string>();

        // Act - Create multiple jobs concurrently
        for (int i = 0; i < 10; i++)
        {
            var job = new JobStatusResponse
            {
                JobId = Guid.NewGuid().ToString("N"),
                Status = "pending",
                Progress = 0,
                CreatedAt = DateTime.UtcNow
            };
            jobIds.Add(job.JobId);
            tasks.Add(_repository.CreateJobAsync(job));
        }

        await Task.WhenAll(tasks);

        // Assert - All jobs should be stored
        foreach (var jobId in jobIds)
        {
            var retrieved = await _repository.GetJobAsync(jobId);
            retrieved.Should().NotBeNull($"Job {jobId} should exist");
        }
    }

    [Fact]
    public async Task UpdateJobAsync_ShouldPreserveJobData_WhenUpdating()
    {
        // Arrange
        var originalJob = new JobStatusResponse
        {
            JobId = Guid.NewGuid().ToString("N"),
            Status = "pending",
            Progress = 0,
            CreatedAt = DateTime.UtcNow,
            OutputPath = null,
            ErrorMessage = null
        };

        await _repository.CreateJobAsync(originalJob);

        // Act
        var updatedJob = originalJob with
        {
            Status = "processing",
            Progress = 25
        };
        await _repository.UpdateJobAsync(updatedJob);

        // Assert
        var retrieved = await _repository.GetJobAsync(originalJob.JobId);
        retrieved.Should().NotBeNull();
        retrieved!.JobId.Should().Be(originalJob.JobId);
        retrieved.CreatedAt.Should().BeCloseTo(originalJob.CreatedAt, TimeSpan.FromSeconds(1));
        retrieved.Status.Should().Be("processing");
        retrieved.Progress.Should().Be(25);
    }

    public void Dispose()
    {
        _repository?.Dispose();
        try
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
