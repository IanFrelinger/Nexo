using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nexo.Spike.S3.Models;

namespace Nexo.Spike.S3.Registry;

public static class RegistryPaths
{
    public const string RegistryFolderName = "registry";
    public const string EntryFileName = "entry.json";
    public const string ImplementationFileName = "implementation.cs";
    public const string CatalogFileName = "catalog.json";

    public static string ResolveDefaultRegistryRoot()
    {
        var candidates = new List<string>();
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            candidates.Add(Path.Combine(dir.FullName, "artifacts", "s3", RegistryFolderName));
            dir = dir.Parent;
        }

        candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "s3", RegistryFolderName));

        foreach (var candidate in candidates)
        {
            var parent = Path.GetDirectoryName(candidate);
            if (parent is not null && Directory.Exists(Path.GetDirectoryName(parent) ?? parent))
                return candidate;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "s3", RegistryFolderName);
    }

    public static string EntryDirectory(string registryRoot, string entryId) =>
        Path.Combine(registryRoot, SanitizeEntryId(entryId));

    public static string EntryFilePath(string registryRoot, string entryId) =>
        Path.Combine(EntryDirectory(registryRoot, entryId), EntryFileName);

    public static string ImplementationFilePath(string registryRoot, string entryId) =>
        Path.Combine(EntryDirectory(registryRoot, entryId), ImplementationFileName);

    public static string CatalogPath(string registryRoot) =>
        Path.Combine(registryRoot, CatalogFileName);

    public static string ComputeContentHash(string implementationSource, IntentDescriptor intent)
    {
        var intentJson = JsonSerializer.Serialize(intent);
        var payload = $"{implementationSource}\n---\n{intentJson}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string SanitizeEntryId(string entryId) =>
        entryId.Replace('/', '-').Replace('\\', '-').Replace(':', '-');
}
