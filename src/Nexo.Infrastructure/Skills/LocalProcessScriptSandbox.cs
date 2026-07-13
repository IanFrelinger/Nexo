using System.Diagnostics;
using System.Text;
using Nexo.Core.Application.Skills.Ports;

namespace Nexo.Infrastructure.Skills;

/// <summary>
/// Local process sandbox with timeout and output-size enforcement.
/// </summary>
public sealed class LocalProcessScriptSandbox : INexoScriptSandbox
{
    /// <inheritdoc />
    public async Task<SandboxScriptResult> RunAsync(
        string scriptAbsolutePath,
        string workingDirectory,
        TimeSpan timeout,
        int maxOutputBytes,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(workingDirectory);

        var extension = Path.GetExtension(scriptAbsolutePath).ToLowerInvariant();
        var (fileName, arguments) = ResolveCommand(scriptAbsolutePath, extension);

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo };
        var stopwatch = Stopwatch.StartNew();

        if (!process.Start())
        {
            return new SandboxScriptResult(
                string.Empty,
                "Failed to start script process.",
                1,
                false,
                stopwatch.Elapsed);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var stdoutTask = ReadLimitedAsync(process.StandardOutput, maxOutputBytes, timeoutCts.Token);
        var stderrTask = ReadLimitedAsync(process.StandardError, maxOutputBytes, timeoutCts.Token);

        var timeoutMs = (int)Math.Clamp(timeout.TotalMilliseconds, 1, int.MaxValue);
        var exited = await Task.Run(() => process.WaitForExit(timeoutMs), cancellationToken).ConfigureAwait(false);
        if (!exited)
        {
            TryKill(process);
            stopwatch.Stop();
            return new SandboxScriptResult(
                string.Empty,
                $"Script timed out after {timeout.TotalSeconds:0}s.",
                124,
                false,
                stopwatch.Elapsed);
        }

        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);
        stopwatch.Stop();

        return new SandboxScriptResult(
            stdout.Text,
            stderr.Text,
            process.ExitCode,
            stdout.Truncated || stderr.Truncated,
            stopwatch.Elapsed);
    }

    private static (string FileName, string Arguments) ResolveCommand(string scriptAbsolutePath, string extension)
        => extension switch
        {
            ".sh" => ("bash", Quote(scriptAbsolutePath)),
            ".py" => ("python3", Quote(scriptAbsolutePath)),
            ".ps1" => ("pwsh", $"-NoProfile -File {Quote(scriptAbsolutePath)}"),
            _ => (scriptAbsolutePath, string.Empty),
        };

    private static string Quote(string value) => $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static async Task<(string Text, bool Truncated)> ReadLimitedAsync(
        StreamReader reader,
        int maxOutputBytes,
        CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        var buffer = new char[256];
        var totalBytes = 0;
        var truncated = false;

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
            if (read <= 0)
                break;

            var chunk = new string(buffer, 0, read);
            var chunkBytes = Encoding.UTF8.GetByteCount(chunk);
            if (totalBytes + chunkBytes > maxOutputBytes)
            {
                var allowed = maxOutputBytes - totalBytes;
                if (allowed > 0)
                {
                    var partial = TruncateToByteLimit(chunk, allowed);
                    builder.Append(partial);
                }

                truncated = true;
                break;
            }

            builder.Append(chunk);
            totalBytes += chunkBytes;
        }

        return (builder.ToString(), truncated);
    }

    private static string TruncateToByteLimit(string value, int maxBytes)
    {
        if (maxBytes <= 0)
            return string.Empty;

        var bytes = Encoding.UTF8.GetBytes(value);
        if (bytes.Length <= maxBytes)
            return value;

        return Encoding.UTF8.GetString(bytes, 0, maxBytes);
    }
}
