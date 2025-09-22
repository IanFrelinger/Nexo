using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Nexo.Agent.Models;
using Nexo.Agent.Abstractions;

namespace Nexo.Agent.Tools.Builtin;

/// <summary>
/// Tool for reading files from the file system.
/// </summary>
public class FileReadTool : ITool<FileReadInput, FileReadOutput>
{
    public ToolManifest Manifest { get; }

    public FileReadTool()
    {
        Manifest = ToolManifest.Create(
            id: "tool.file.read",
            name: "File Read",
            description: "Read text or binary files from the file system",
            requiredPermissions: ToolPermissions.FileRead,
            capabilities: new[] { "file", "read", "io" },
            timeout: TimeSpan.FromMinutes(2)
        );
    }

    public async Task<string> ExecuteAsync(string input, ToolContext context, CancellationToken cancellationToken = default)
    {
        var typedInput = JsonSerializer.Deserialize<FileReadInput>(input) ?? throw new ArgumentException("Invalid input");
        var result = await ExecuteAsync(typedInput, context, cancellationToken);
        return JsonSerializer.Serialize(result);
    }

    public async Task<FileReadOutput> ExecuteAsync(FileReadInput input, ToolContext context, CancellationToken cancellationToken = default)
    {
        using var activity = new ActivitySource("Nexo.Tool").StartActivity("FileRead");
        activity?.SetTag("tool.file.path", input.FilePath);
        activity?.SetTag("tool.file.binary", input.ReadAsBinary);

        try
        {
            context.Logger.LogInformation("Reading file: {FilePath}", input.FilePath);

            // Check if file exists
            if (!context.FileSystem.FileExists(input.FilePath))
            {
                var error = $"File not found: {input.FilePath}";
                context.Logger.LogError(error);
                activity?.SetStatus(ActivityStatusCode.Error, error);
                return new FileReadOutput(false, null, error, 0);
            }

            // Read file content
            string content;
            long fileSize;

            if (input.ReadAsBinary)
            {
                // For binary files, read as base64
                var bytes = await context.FileSystem.ReadAllBytesAsync(input.FilePath, cancellationToken);
                content = Convert.ToBase64String(bytes);
                fileSize = bytes.Length;
            }
            else
            {
                // For text files, read as text
                content = await context.FileSystem.ReadAllTextAsync(input.FilePath, cancellationToken);
                fileSize = context.FileSystem.GetFileSize(input.FilePath);
            }

            context.Logger.LogInformation("Successfully read file {FilePath} ({Size} bytes)", input.FilePath, fileSize);
            activity?.SetTag("tool.file.size", fileSize);

            return new FileReadOutput(true, content, null, fileSize);
        }
        catch (Exception ex)
        {
            var error = $"Error reading file {input.FilePath}: {ex.Message}";
            context.Logger.LogError(ex, error);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return new FileReadOutput(false, null, error, 0);
        }
    }
}

/// <summary>
/// Input for the FileRead tool.
/// </summary>
/// <param name="FilePath">Path to the file to read</param>
/// <param name="ReadAsBinary">Whether to read the file as binary (base64 encoded)</param>
public record FileReadInput(string FilePath, bool ReadAsBinary = false);

/// <summary>
/// Output from the FileRead tool.
/// </summary>
/// <param name="Success">Whether the file was read successfully</param>
/// <param name="Content">File content (text or base64 encoded binary)</param>
/// <param name="Error">Error message if reading failed</param>
/// <param name="FileSize">Size of the file in bytes</param>
public record FileReadOutput(bool Success, string? Content, string? Error, long FileSize);
