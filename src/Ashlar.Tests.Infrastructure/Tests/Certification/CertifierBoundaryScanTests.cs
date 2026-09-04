using FluentAssertions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// B5: hermetic Mono.Cecil scan of the certifier. <c>Assembly.LoadFrom</c> /
/// <c>LoadFromAssemblyPath</c> / <c>Activator.CreateInstance</c> may appear only on
/// the frozen inventory. A new call site is a regression, not a silent exception.
/// </summary>
[Trait("Category", "Certification")]
public sealed class CertifierBoundaryScanTests
{
    private static readonly string InventoryPath = Path.Combine(
        FindRepoRoot(),
        "ci",
        "certifier-boundary-inventory.tsv");

    private static readonly HashSet<string> WatchedApis = new(StringComparer.Ordinal)
    {
        "System.Reflection.Assembly::LoadFrom",
        "System.Reflection.Assembly::LoadFile",
        "System.Runtime.Loader.AssemblyLoadContext::LoadFromAssemblyPath",
        "System.Activator::CreateInstance"
    };

    [Fact]
    public void CertifierAssemblies_LoadAndCreateInstance_StayInsideFrozenInventory()
    {
        File.Exists(InventoryPath).Should().BeTrue("ci/certifier-boundary-inventory.tsv is the B5 freeze");
        var allowed = File.ReadAllLines(InventoryPath)
            .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith('#'))
            .Select(ParseInventoryRow)
            .ToHashSet(StringComparer.Ordinal);

        var hits = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var path in CertifierAssemblies())
        {
            using var module = ModuleDefinition.ReadModule(path);
            foreach (var type in module.Types.SelectMany(Flatten))
            {
                if (!type.FullName.StartsWith("Ashlar.Infrastructure.Certification", StringComparison.Ordinal)
                    && !type.FullName.StartsWith("Program", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (var method in type.Methods.Where(m => m.HasBody))
                {
                    foreach (var instruction in method.Body.Instructions)
                    {
                        if (instruction.Operand is not MethodReference callee)
                            continue;
                        var api = callee.DeclaringType.FullName + "::" + callee.Name;
                        if (!WatchedApis.Contains(api))
                            continue;
                        hits.Add($"{type.FullName}::{method.Name}\t{api}");
                    }
                }
            }
        }

        var unexpected = hits.Where(h => !allowed.Contains(h)).ToArray();
        var stale = allowed.Where(a => !hits.Contains(a)).ToArray();
        unexpected.Should().BeEmpty(
            "new certifier LoadFrom/CreateInstance sites must be added to ci/certifier-boundary-inventory.tsv with a reason. Unexpected:\n"
            + string.Join("\n", unexpected));
        stale.Should().BeEmpty(
            "inventory lists sites that no longer exist; shrink the list, do not keep ghosts:\n"
            + string.Join("\n", stale));
    }

    private static IEnumerable<TypeDefinition> Flatten(TypeDefinition type) =>
        type.NestedTypes.SelectMany(Flatten).Prepend(type);

    private static string ParseInventoryRow(string line)
    {
        var parts = line.Split('\t');
        parts.Length.Should().BeGreaterThanOrEqualTo(2, $"inventory row must be site<TAB>api<TAB>reason: {line}");
        return parts[0] + "\t" + parts[1];
    }

    private static IEnumerable<string> CertifierAssemblies()
    {
        var dir = AppContext.BaseDirectory;
        foreach (var name in new[] { "Ashlar.Infrastructure.dll" })
        {
            var path = Path.Combine(dir, name);
            if (File.Exists(path))
                yield return path;
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Ashlar.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repo root from " + AppContext.BaseDirectory);
    }
}
