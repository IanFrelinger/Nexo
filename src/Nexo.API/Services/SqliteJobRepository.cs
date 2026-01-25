using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Nexo.API.Models;
using System.Text.Json;

namespace Nexo.API.Services;

/// <summary>
/// SQLite-based implementation of job repository.
/// </summary>
public class SqliteJobRepository : IJobRepository, IDisposable
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteJobRepository> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public SqliteJobRepository(string dbPath, ILogger<SqliteJobRepository> logger)
    {
        _connectionString = $"Data Source={dbPath}";
        _logger = logger;
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS jobs (
                    job_id TEXT PRIMARY KEY,
                    status TEXT NOT NULL,
                    progress INTEGER NOT NULL,
                    error_message TEXT,
                    output_path TEXT,
                    created_at TEXT NOT NULL,
                    completed_at TEXT,
                    job_data TEXT NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_jobs_created_at ON jobs(created_at);
                CREATE INDEX IF NOT EXISTS idx_jobs_status ON jobs(status);
            ";

            command.ExecuteNonQuery();
            _logger.LogInformation("Job database initialized at {DbPath}", _connectionString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize job database");
            throw;
        }
    }

    public async Task<string> CreateJobAsync(JobStatusResponse job, CancellationToken ct = default)
    {
        if (_semaphore == null) return job.JobId; // Disposed
        await _semaphore.WaitAsync(ct);
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO jobs (job_id, status, progress, error_message, output_path, created_at, completed_at, job_data)
                VALUES ($jobId, $status, $progress, $errorMessage, $outputPath, $createdAt, $completedAt, $jobData)
            ";

            var jobData = JsonSerializer.Serialize(job);

            command.Parameters.AddWithValue("$jobId", job.JobId);
            command.Parameters.AddWithValue("$status", job.Status);
            command.Parameters.AddWithValue("$progress", job.Progress);
            command.Parameters.AddWithValue("$errorMessage", (object?)job.ErrorMessage ?? DBNull.Value);
            command.Parameters.AddWithValue("$outputPath", (object?)job.OutputPath ?? DBNull.Value);
            command.Parameters.AddWithValue("$createdAt", job.CreatedAt.ToString("O"));
            command.Parameters.AddWithValue("$completedAt", job.CompletedAt?.ToString("O") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$jobData", jobData);

            await command.ExecuteNonQueryAsync(ct);
            return job.JobId;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<JobStatusResponse?> GetJobAsync(string jobId, CancellationToken ct = default)
    {
        if (_semaphore == null) return null; // Disposed
        await _semaphore.WaitAsync(ct);
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);

            var command = connection.CreateCommand();
            command.CommandText = "SELECT job_data FROM jobs WHERE job_id = $jobId";
            command.Parameters.AddWithValue("$jobId", jobId);

            using var reader = await command.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                var jobData = reader.GetString(0);
                return JsonSerializer.Deserialize<JobStatusResponse>(jobData);
            }

            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task UpdateJobAsync(JobStatusResponse job, CancellationToken ct = default)
    {
        if (_semaphore == null) return; // Disposed
        await _semaphore.WaitAsync(ct);
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE jobs 
                SET status = $status, 
                    progress = $progress, 
                    error_message = $errorMessage, 
                    output_path = $outputPath, 
                    completed_at = $completedAt,
                    job_data = $jobData
                WHERE job_id = $jobId
            ";

            var jobData = JsonSerializer.Serialize(job);

            command.Parameters.AddWithValue("$jobId", job.JobId);
            command.Parameters.AddWithValue("$status", job.Status);
            command.Parameters.AddWithValue("$progress", job.Progress);
            command.Parameters.AddWithValue("$errorMessage", (object?)job.ErrorMessage ?? DBNull.Value);
            command.Parameters.AddWithValue("$outputPath", (object?)job.OutputPath ?? DBNull.Value);
            command.Parameters.AddWithValue("$completedAt", job.CompletedAt?.ToString("O") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$jobData", jobData);

            await command.ExecuteNonQueryAsync(ct);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IReadOnlyList<JobStatusResponse>> GetJobsAsync(int limit = 100, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);

            var command = connection.CreateCommand();
            command.CommandText = "SELECT job_data FROM jobs ORDER BY created_at DESC LIMIT $limit";
            command.Parameters.AddWithValue("$limit", limit);

            var jobs = new List<JobStatusResponse>();

            using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var jobData = reader.GetString(0);
                var job = JsonSerializer.Deserialize<JobStatusResponse>(jobData);
                if (job != null)
                {
                    jobs.Add(job);
                }
            }

            return jobs;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task DeleteJobAsync(string jobId, CancellationToken ct = default)
    {
        if (_semaphore == null) return; // Disposed
        await _semaphore.WaitAsync(ct);
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM jobs WHERE job_id = $jobId";
            command.Parameters.AddWithValue("$jobId", jobId);

            await command.ExecuteNonQueryAsync(ct);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task DeleteOldJobsAsync(TimeSpan olderThan, CancellationToken ct = default)
    {
        if (_semaphore == null) return; // Disposed
        await _semaphore.WaitAsync(ct);
        try
        {
            var cutoffDate = DateTime.UtcNow - olderThan;

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM jobs WHERE created_at < $cutoffDate";
            command.Parameters.AddWithValue("$cutoffDate", cutoffDate.ToString("O"));

            var deleted = await command.ExecuteNonQueryAsync(ct);
            _logger.LogInformation("Deleted {Count} old jobs (older than {CutoffDate})", deleted, cutoffDate);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
    }
}
