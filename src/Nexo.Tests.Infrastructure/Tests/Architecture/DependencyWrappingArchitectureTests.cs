using Mono.Cecil;
using Mono.Cecil.Cil;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Infrastructure.Tests.Architecture;

/// <summary>
/// Enforces "only platform-specific top layer can have raw calls".
///
/// Rule:
/// - Raw platform dependencies (System.IO.File/Directory, Process, HttpClient) are only allowed
///   inside platform-specific namespaces (Tools/Adapters/Infrastructure).
/// - Everywhere else must call through ports/tools/adapters.
/// </summary>
public sealed class DependencyWrappingArchitectureTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Scan all Nexo assemblies in the current test output directory (excluding test assemblies)
            // so the rule covers the entire built product, not just a couple projects.
            AssertOnlyPlatformNamespacesUseForbiddenApis(AppContext.BaseDirectory);

            return Task.FromResult(new TestResult
            {
                TestName = nameof(DependencyWrappingArchitectureTests),
                Category = "Architecture",
                Passed = true,
                Message = "Raw platform dependencies are confined to platform-specific namespaces"
            });
        }
        catch (AssertionException ex)
        {
            return Task.FromResult(new TestResult
            {
                TestName = nameof(DependencyWrappingArchitectureTests),
                Category = "Architecture",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                TestName = nameof(DependencyWrappingArchitectureTests),
                Category = "Architecture",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            });
        }
    }

    private void AssertOnlyPlatformNamespacesUseForbiddenApis(string baseDirectory)
    {
        var dlls = Directory.GetFiles(baseDirectory, "Nexo*.dll", SearchOption.TopDirectoryOnly)
            .Where(p =>
                !Path.GetFileName(p).StartsWith("Nexo.Tests.", StringComparison.OrdinalIgnoreCase)
                && !Path.GetFileName(p).Contains("TestHost", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Platform-specific namespaces are allowed to call raw APIs.
        // Everything else should call through ports/tools/adapters.
        // "Top layer with platform specifics" allowlist:
        // - Tools (process execution, compilation, decompilation, etc.)
        // - Infrastructure (IO/network/runtime integrations)
        // - Adapters (platform-specific integration points)
        // - CLI (host/platform entrypoint)
        // - Orchestration agents that are explicitly platform-facing
        // STRICT: raw platform APIs are only allowed in platform-specific top layers.
        // Everything else must go through ports/tools/adapters.
        var allowedNamespacePrefixes = new[]
        {
            "Nexo.Tools",
            "Nexo.Infrastructure"
        };

        // Forbidden raw platform APIs (type full names)
        var forbiddenTypeNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "System.IO.File",
            "System.IO.Directory",
            "System.IO.FileStream",
            "System.IO.StreamReader",
            "System.IO.StreamWriter",
            "System.Diagnostics.Process",
            "System.Diagnostics.ProcessStartInfo",
            "System.Net.Http.HttpClient",
            "System.Net.Http.HttpRequestMessage",
            "System.Net.Http.HttpResponseMessage"
        };

        var offenders = new List<string>();

        foreach (var dll in dlls)
        {
            using var module = ModuleDefinition.ReadModule(dll);
            foreach (var type in module.Types)
            {
                ScanType(type, module.FileName, allowedNamespacePrefixes, forbiddenTypeNames, offenders);
            }
        }

        AssertTrue(offenders.Count == 0,
            $"Raw platform APIs used outside platform namespaces:\n- {string.Join("\n- ", offenders.Take(60))}");
    }

    private static void ScanType(
        TypeDefinition type,
        string assemblyFile,
        string[] allowedNamespacePrefixes,
        HashSet<string> forbiddenTypeNames,
        List<string> offenders)
    {
        if (type == null) return;

        var ns = GetEffectiveNamespace(type);
        var isAllowed =
            allowedNamespacePrefixes.Any(p => IsInNamespace(ns, p)) ||
            // Any Adapters namespace is considered platform-specific
            ns.Contains(".Adapters", StringComparison.Ordinal);

        // Scan method bodies for direct calls/newobj/etc that reference forbidden types.
        foreach (var method in type.Methods)
        {
            if (!method.HasBody) continue;
            foreach (var ins in method.Body.Instructions)
            {
                if (ins.OpCode == OpCodes.Call || ins.OpCode == OpCodes.Callvirt || ins.OpCode == OpCodes.Newobj)
                {
                    if (ins.Operand is MethodReference mr)
                    {
                        var dt = mr.DeclaringType?.FullName;
                        if (dt != null && forbiddenTypeNames.Contains(dt) && !isAllowed)
                        {
                            offenders.Add($"{Path.GetFileName(assemblyFile)} :: {type.FullName}.{method.Name} -> {dt}.{mr.Name}");
                        }
                    }
                }
                else if (ins.OpCode == OpCodes.Ldsfld || ins.OpCode == OpCodes.Ldfld)
                {
                    if (ins.Operand is FieldReference fr)
                    {
                        var dt = fr.DeclaringType?.FullName;
                        if (dt != null && forbiddenTypeNames.Contains(dt) && !isAllowed)
                        {
                            offenders.Add($"{Path.GetFileName(assemblyFile)} :: {type.FullName}.{method.Name} -> field on {dt}");
                        }
                    }
                }
            }
        }

        // Recurse nested types
        foreach (var nested in type.NestedTypes)
        {
            ScanType(nested, assemblyFile, allowedNamespacePrefixes, forbiddenTypeNames, offenders);
        }
    }

    private static string GetEffectiveNamespace(TypeDefinition type)
    {
        // Cecil can report empty Namespace for some nested/compiler-generated types.
        // Walk up to find the first non-empty namespace.
        var cur = type;
        while (cur != null)
        {
            if (!string.IsNullOrWhiteSpace(cur.Namespace)) return cur.Namespace;
            cur = cur.DeclaringType;
        }
        return "";
    }

    private static bool IsInNamespace(string ns, string prefix)
    {
        if (string.IsNullOrEmpty(prefix)) return false;
        if (string.Equals(ns, prefix, StringComparison.Ordinal)) return true;
        return ns.StartsWith(prefix + ".", StringComparison.Ordinal);
    }
}

