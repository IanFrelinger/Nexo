namespace Nexo.Core.Application.Common.Ports;

/// <summary>
/// Abstraction for text file IO so Application code does not depend on System.IO directly.
/// Implementations live in Infrastructure.
/// </summary>
public interface ITextFileSystem
{
    Task<string> ReadAllTextAsync(string path, CancellationToken ct = default);
    Task WriteAllTextAsync(string path, string content, CancellationToken ct = default);
    Task WriteAllBytesAsync(string path, byte[] content, CancellationToken ct = default);
}

