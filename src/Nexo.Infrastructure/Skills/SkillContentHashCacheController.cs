using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Nexo.Core.Application.Skills.Ports;

namespace Nexo.Infrastructure.Skills;

/// <summary>
/// Content-hash based skill cache invalidation controller.
/// </summary>
public sealed class SkillContentHashCacheController : INexoSkillCacheController
{
    private readonly ConcurrentDictionary<string, string> _hashes = new(StringComparer.OrdinalIgnoreCase);
    private int _globalGeneration;

    /// <inheritdoc />
    public void InvalidateAll() => Interlocked.Increment(ref _globalGeneration);

    /// <inheritdoc />
    public void InvalidateIfContentChanged(string skillsRootPath)
    {
        if (string.IsNullOrWhiteSpace(skillsRootPath))
            return;

        var fullPath = Path.GetFullPath(skillsRootPath);
        var hash = ComputeContentHash(fullPath);
        _hashes.AddOrUpdate(fullPath, hash, (_, previous) =>
        {
            if (!string.Equals(previous, hash, StringComparison.Ordinal))
                Interlocked.Increment(ref _globalGeneration);
            return hash;
        });
    }

    /// <inheritdoc />
    public string ComputeContentHash(string skillsRootPath)
    {
        if (string.IsNullOrWhiteSpace(skillsRootPath) || !Directory.Exists(skillsRootPath))
            return string.Empty;

        var builder = new StringBuilder();
        builder.Append(_globalGeneration);
        builder.Append('|');

        foreach (var file in Directory.EnumerateFiles(skillsRootPath, "*", SearchOption.AllDirectories)
                     .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase))
        {
            var info = new FileInfo(file);
            builder.Append(info.FullName);
            builder.Append(':');
            builder.Append(info.Length);
            builder.Append(':');
            builder.Append(info.LastWriteTimeUtc.Ticks);
            builder.Append(';');
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(bytes);
    }
}
