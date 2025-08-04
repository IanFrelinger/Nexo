using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Interfaces;

namespace Nexo.Feature.Analysis.Services
{
    /// <summary>
    /// Service for detecting Git changes to enable intelligent test selection.
    /// </summary>
    public class GitChangeDetector : IGitChangeDetector
    {
        private readonly ILogger<GitChangeDetector> _logger;

        public GitChangeDetector(ILogger<GitChangeDetector> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<string>> GetChangedFilesAsync(string sinceReference, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sinceReference))
            {
                throw new ArgumentException("sinceReference cannot be null or empty", nameof(sinceReference));
            }

            _logger.LogDebug("Getting changed files since: {SinceReference}", sinceReference);

            try
            {
                if (!await IsGitRepositoryAsync(cancellationToken))
                {
                    _logger.LogWarning("Current directory is not a Git repository");
                    return new List<string>();
                }

                var command = $"diff --name-only {sinceReference}";
                var result = await ExecuteGitCommandAsync(command, cancellationToken);

                if (result.Success)
                {
                    var files = result.Output
                        .Split('\n')
                        .Where(f => !string.IsNullOrWhiteSpace(f))
                        .ToList();

                    _logger.LogDebug("Found {Count} changed files", files.Count);
                    return files;
                }
                else
                {
                    _logger.LogError("Failed to get changed files: {Error}", result.Error);
                    return new List<string>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting changed files since {SinceReference}", sinceReference);
                return new List<string>();
            }
        }

        public async Task<List<string>> GetUncommittedChangesAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting uncommitted changes");

            try
            {
                if (!await IsGitRepositoryAsync(cancellationToken))
                {
                    _logger.LogWarning("Current directory is not a Git repository");
                    return new List<string>();
                }

                var command = "diff --name-only HEAD";
                var result = await ExecuteGitCommandAsync(command, cancellationToken);

                if (result.Success)
                {
                    var files = result.Output
                        .Split('\n')
                        .Where(f => !string.IsNullOrWhiteSpace(f))
                        .ToList();

                    _logger.LogDebug("Found {Count} uncommitted changes", files.Count);
                    return files;
                }
                else
                {
                    _logger.LogError("Failed to get uncommitted changes: {Error}", result.Error);
                    return new List<string>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting uncommitted changes");
                return new List<string>();
            }
        }

        public async Task<List<string>> GetChangedFilesBetweenCommitsAsync(string fromCommit, string toCommit, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fromCommit))
            {
                throw new ArgumentException("fromCommit cannot be null or empty", nameof(fromCommit));
            }

            if (string.IsNullOrWhiteSpace(toCommit))
            {
                throw new ArgumentException("toCommit cannot be null or empty", nameof(toCommit));
            }

            _logger.LogDebug("Getting changed files between commits: {FromCommit} -> {ToCommit}", fromCommit, toCommit);

            try
            {
                if (!await IsGitRepositoryAsync(cancellationToken))
                {
                    _logger.LogWarning("Current directory is not a Git repository");
                    return new List<string>();
                }

                var command = $"diff --name-only {fromCommit} {toCommit}";
                var result = await ExecuteGitCommandAsync(command, cancellationToken);

                if (result.Success)
                {
                    var files = result.Output
                        .Split('\n')
                        .Where(f => !string.IsNullOrWhiteSpace(f))
                        .ToList();

                    _logger.LogDebug("Found {Count} changed files between commits", files.Count);
                    return files;
                }
                else
                {
                    _logger.LogError("Failed to get changed files between commits: {Error}", result.Error);
                    return new List<string>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting changed files between commits {FromCommit} -> {ToCommit}", fromCommit, toCommit);
                return new List<string>();
            }
        }

        public async Task<bool> IsGitRepositoryAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await ExecuteGitCommandAsync("rev-parse --git-dir", cancellationToken);
                return result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error checking if directory is Git repository");
                return false;
            }
        }

        public async Task<string> GetCurrentBranchAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await ExecuteGitCommandAsync("rev-parse --abbrev-ref HEAD", cancellationToken);
                return result.Success ? result.Output.Trim() : string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current branch");
                return string.Empty;
            }
        }

        public async Task<string> GetLatestCommitHashAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await ExecuteGitCommandAsync("rev-parse HEAD", cancellationToken);
                return result.Success ? result.Output.Trim() : string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest commit hash");
                return string.Empty;
            }
        }

        private async Task<GitCommandResult> ExecuteGitCommandAsync(string arguments, CancellationToken cancellationToken)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = Directory.GetCurrentDirectory(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process process = null;
            try
            {
                process = new Process { StartInfo = startInfo };
                var output = new StringBuilder();
                var error = new StringBuilder();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        output.AppendLine(e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        error.AppendLine(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var completed = await Task.Run(() => process.WaitForExit(30000), cancellationToken); // 30 second timeout

                if (!completed)
                {
                    process.Kill();
                    return new GitCommandResult
                    {
                        Success = false,
                        Error = "Command timed out"
                    };
                }

                return new GitCommandResult
                {
                    Success = process.ExitCode == 0,
                    Output = output.ToString(),
                    Error = error.ToString(),
                    ExitCode = process.ExitCode
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Git command: {Arguments}", arguments);
                return new GitCommandResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
            finally
            {
                process?.Dispose();
            }
        }

        private class GitCommandResult
        {
            public bool Success { get; set; }
            public string Output { get; set; } = string.Empty;
            public string Error { get; set; } = string.Empty;
            public int ExitCode { get; set; }
        }
    }
}