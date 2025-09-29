namespace Nexo.Tests.Contracts;

/// <summary>
/// Temporary workspace for testing file operations
/// </summary>
public sealed class TempWorkspace : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nexo_" + Guid.NewGuid().ToString("N"));
    
    public TempWorkspace() => Directory.CreateDirectory(Path);
    
    public void Dispose() 
    { 
        try 
        { 
            Directory.Delete(Path, true); 
        } 
        catch 
        { 
            // ignore cleanup errors
        } 
    }
}
