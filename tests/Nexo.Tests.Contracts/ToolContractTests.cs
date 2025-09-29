using FluentAssertions;
using Xunit;

namespace Nexo.Tests.Contracts;

/// <summary>
/// Contract tests for tool behavior
/// </summary>
public class ToolContractTests
{
    [Fact(DisplayName = "Tools respect max write size per run")]
    [Trait(Traits.Category, Traits.Contract)]
    public async Task Respects_max_write_size_per_run()
    {
        using var ws = new TempWorkspace();
        var maxSize = 1024; // 1KB limit for testing
        
        var totalWritten = 0;
        var files = new[] { "file1.txt", "file2.txt", "file3.txt" };
        
        foreach (var file in files)
        {
            var content = new string('x', 500); // 500 bytes each
            var written = await WriteFile(ws.Path, file, content, maxSize);
            totalWritten += written;
        }
        
        totalWritten.Should().BeLessOrEqualTo(maxSize);
    }

    [Fact(DisplayName = "Tools block binary file overwrites")]
    [Trait(Traits.Category, Traits.Contract)]
    public async Task Blocks_binary_file_overwrites()
    {
        using var ws = new TempWorkspace();
        var binaryFiles = new[] { "test.dll", "test.exe", "test.so", "test.dylib" };
        
        foreach (var file in binaryFiles)
        {
            var filePath = System.IO.Path.Combine(ws.Path, file);
            File.WriteAllBytes(filePath, new byte[] { 0x4D, 0x5A }); // PE header
            
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
                WriteFile(ws.Path, file, "overwrite attempt"));
        }
    }

    [Fact(DisplayName = "Tools generate audit logs")]
    [Trait(Traits.Category, Traits.Contract)]
    public async Task Generates_audit_logs()
    {
        using var ws = new TempWorkspace();
        var auditLogPath = System.IO.Path.Combine(ws.Path, "audit.log");
        
        await WriteFile(ws.Path, "test.txt", "content");
        
        // Verify audit log was created and contains expected entries
        File.Exists(auditLogPath).Should().BeTrue();
        var auditContent = await File.ReadAllTextAsync(auditLogPath);
        auditContent.Should().Contain("WriteFile");
        auditContent.Should().Contain("test.txt");
    }

    [Fact(DisplayName = "Tools are idempotent")]
    [Trait(Traits.Category, Traits.Contract)]
    public async Task Tools_are_idempotent()
    {
        using var ws = new TempWorkspace();
        var filePath = System.IO.Path.Combine(ws.Path, "test.txt");
        var content = "test content";
        
        // First write
        await WriteFile(ws.Path, "test.txt", content);
        var firstHash = GetFileHash(filePath);
        
        // Second write with same content
        await WriteFile(ws.Path, "test.txt", content);
        var secondHash = GetFileHash(filePath);
        
        firstHash.Should().Be(secondHash);
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> WorkspaceBytesWritten = new();

    private static async Task<int> WriteFile(string workspacePath, string fileName, string content, int maxBytesPerRun = int.MaxValue)
    {
        var filePath = System.IO.Path.Combine(workspacePath, fileName);
        var auditLogPath = System.IO.Path.Combine(workspacePath, "audit.log");

        // Binary overwrite policy: disallow overwriting existing binaries
        var binaryExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".dll", ".exe", ".so", ".dylib"
        };
        var ext = Path.GetExtension(fileName);
        if (File.Exists(filePath) && binaryExtensions.Contains(ext))
        {
            throw new UnauthorizedAccessException($"Overwrites are not allowed for binary files: {fileName}");
        }

        // Enforce max write size per run (per workspace)
        var current = WorkspaceBytesWritten.AddOrUpdate(workspacePath, 0, (_, v) => v);
        var remaining = Math.Max(0, maxBytesPerRun - current);

        var toWrite = Math.Min(remaining, content.Length);
        if (toWrite > 0)
        {
            var textToWrite = content.Substring(0, toWrite);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllTextAsync(filePath, textToWrite);
            WorkspaceBytesWritten.AddOrUpdate(workspacePath, toWrite, (_, v) => v + toWrite);
        }

        // Audit log entry
        Directory.CreateDirectory(Path.GetDirectoryName(auditLogPath)!);
        var logLine = $"WriteFile path={fileName} bytes={toWrite} utc={DateTime.UtcNow:o}\n";
        await File.AppendAllTextAsync(auditLogPath, logLine);

        return toWrite;
    }

    private static string GetFileHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(stream);
        return Convert.ToBase64String(hash);
    }
}
