using Mono.Cecil;
using Mono.Cecil.Cil;
using Ashlar.Certification.Contracts;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// Type-level import fence applied to a gate-emitted assembly before anything loads it.
/// Allowlisted assemblies plus a denylist of execution-context and reflection/process types.
/// The inventory blob is hashed into the certificate so a fence change is a new judge.
/// </summary>
public static class IlImportFence
{
    /// <summary>Canonical inventory recorded as the <c>il-import-fence</c> input.</summary>
    public const string InventoryBlob =
        "allow=System|System.*|netstandard|mscorlib|Ashlar.*;"
        + "deny=System.Diagnostics.Process|System.Reflection.Assembly|System.Activator|"
        + "System.Runtime.InteropServices.Marshal|Microsoft.CSharp.RuntimeBinder.Binder|"
        + "System.Reflection.Emit;deny-members=System.Environment.Exit|System.Environment.FailFast|"
        + "System.Type.GetType;deny-iface=Ashlar.Core.Domain.Execution.IExecutionContext";

    private static readonly string[] AllowedAssemblyPrefixes =
    [
        "System",
        "System.",
        "netstandard",
        "mscorlib",
        "Ashlar."
    ];

    private static readonly HashSet<string> DeniedTypes = new(StringComparer.Ordinal)
    {
        "System.Diagnostics.Process",
        "System.Reflection.Assembly",
        "System.Activator",
        "System.Runtime.InteropServices.Marshal",
        "Microsoft.CSharp.RuntimeBinder.Binder"
    };

    private static readonly HashSet<string> DeniedMembers = new(StringComparer.Ordinal)
    {
        "System.Environment::Exit",
        "System.Environment::FailFast",
        "System.Type::GetType"
    };

    private const string ExecutionContextInterface = "Ashlar.Core.Domain.Execution.IExecutionContext";

    /// <summary>Signed input naming this fence inventory.</summary>
    public static CertificationInput ToInput() => new()
    {
        Kind = CertificationInputKinds.IlImportFence,
        Id = "type-level-v1",
        Hash = BrickContentHasher.ComputeSha256(InventoryBlob)
    };

    /// <summary>Inspects <paramref name="assemblyBytes"/> and throws on a forbidden import.</summary>
    public static void Inspect(byte[] assemblyBytes)
    {
        ArgumentNullException.ThrowIfNull(assemblyBytes);
        using var module = ModuleDefinition.ReadModule(new MemoryStream(assemblyBytes, writable: false));

        foreach (var type in module.Types)
            InspectType(type);
    }

    private static void InspectType(TypeDefinition type)
    {
        if (type.Name == "CertAuditContext")
        {
            // Injected by CandidateSourceWrapper; the certifier owns it.
        }
        else if (ImplementsExecutionContext(type))
        {
            throw new InvalidOperationException(
                $"il-import fence: type '{type.FullName}' implements {ExecutionContextInterface}. "
                + "Author-supplied execution contexts are refused.");
        }

        foreach (var nested in type.NestedTypes)
            InspectType(nested);

        foreach (var method in type.Methods)
            InspectMethod(method);
    }

    private static bool ImplementsExecutionContext(TypeDefinition type)
    {
        foreach (var iface in type.Interfaces)
        {
            if (string.Equals(iface.InterfaceType.FullName, ExecutionContextInterface, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static void InspectMethod(MethodDefinition method)
    {
        if (!method.HasBody)
            return;

        foreach (var instruction in method.Body.Instructions)
        {
            if (instruction.Operand is not MemberReference member)
                continue;
            if (instruction.OpCode != OpCodes.Call
                && instruction.OpCode != OpCodes.Callvirt
                && instruction.OpCode != OpCodes.Newobj
                && instruction.OpCode != OpCodes.Ldftn
                && instruction.OpCode != OpCodes.Ldvirtftn)
            {
                continue;
            }

            var declaring = member.DeclaringType;
            if (declaring is null)
                continue;

            var assemblyName = declaring.Scope?.Name ?? string.Empty;
            if (!IsAllowedAssembly(assemblyName))
            {
                throw new InvalidOperationException(
                    $"il-import fence: '{method.FullName}' imports '{declaring.FullName}' from "
                    + $"assembly '{assemblyName}', which is outside the allowlist.");
            }

            if (DeniedTypes.Contains(declaring.FullName))
            {
                throw new InvalidOperationException(
                    $"il-import fence: '{method.FullName}' imports denied type '{declaring.FullName}'.");
            }

            var memberKey = declaring.FullName + "::" + member.Name;
            if (DeniedMembers.Contains(memberKey))
            {
                throw new InvalidOperationException(
                    $"il-import fence: '{method.FullName}' calls denied member '{memberKey}'.");
            }
        }
    }

    private static bool IsAllowedAssembly(string assemblyName)
    {
        if (string.IsNullOrEmpty(assemblyName))
            return true;
        if (string.Equals(assemblyName, GateEmittedArtifactCompiler.AssemblyName, StringComparison.Ordinal))
            return true;

        foreach (var prefix in AllowedAssemblyPrefixes)
        {
            if (prefix.EndsWith('.'))
            {
                if (assemblyName.StartsWith(prefix, StringComparison.Ordinal)
                    || string.Equals(assemblyName, prefix.TrimEnd('.'), StringComparison.Ordinal))
                    return true;
            }
            else if (string.Equals(assemblyName, prefix, StringComparison.Ordinal)
                     || assemblyName.StartsWith(prefix + ".", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
