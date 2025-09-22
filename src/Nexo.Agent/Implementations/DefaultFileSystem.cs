using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Agent.Abstractions;

namespace Nexo.Agent.Implementations;

/// <summary>
/// Default implementation of the file system abstraction.
/// </summary>
public class DefaultFileSystem : IFileSystem
{
    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public async Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
    {
        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    public async Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
    {
        await File.WriteAllTextAsync(path, contents, cancellationToken);
    }

    public async Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default)
    {
        return await File.ReadAllBytesAsync(path, cancellationToken);
    }

    public long GetFileSize(string path)
    {
        var fileInfo = new FileInfo(path);
        return fileInfo.Exists ? fileInfo.Length : 0;
    }

    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }
}
