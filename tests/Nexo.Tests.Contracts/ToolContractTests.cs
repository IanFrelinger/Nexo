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
        
        // TODO: Configure MaxWriteSize policy
        var totalWritten = 0;
        var files = new[] { "file1.txt", "file2.txt", "file3.txt" };
        
        foreach (var file in files)
        {
            var content = new string('x', 500); // 500 bytes each
            await WriteFile(ws.Path, file, content);
            totalWritten += content.Length;
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
        
        // TODO: Configure audit logging
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

    private static async Task WriteFile(string workspacePath, string fileName, string content)
    {
        var filePath = System.IO.Path.Combine(workspacePath, fileName);
        
        // TODO: Check PathAllowlist policy before writing
        // TODO: Check MaxWriteSize policy
        // TODO: Check binary file overwrite policy
        // TODO: Write to audit log
        
        await File.WriteAllTextAsync(filePath, content);
    }

    private static string GetFileHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(stream);
        return Convert.ToBase64String(hash);
    }
}
