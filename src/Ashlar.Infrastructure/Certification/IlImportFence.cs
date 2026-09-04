using Mono.Cecil;
using Mono.Cecil.Cil;
using Ashlar.Certification.Contracts;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// Import fence applied to a gate-emitted assembly before anything loads it.
///
/// <para><b>This is an allowlist, deliberately.</b> An earlier version allowed every
/// <c>System.*</c> assembly and denied a handful of named types; an attack pass walked
/// through it five different ways — <c>typeof(Environment).GetMethod("Exit").Invoke(…)</c>
/// reached the denied member reflectively, and <c>Environment.GetEnvironmentVariable</c>
/// read the certifier's own HMAC signing key. A deterministic brick needs a small,
/// enumerable slice of the framework, so the fence names that slice and refuses the rest.
/// The API surface a brick may import is: primitives and math, collections, LINQ, string
/// and text, globalization, tasks and cancellation, the compiler's own async/interpolation
/// plumbing, and the Ashlar brick contracts.</para>
///
/// <para><b>Why it is load-bearing.</b> The witness, determinism, and mutation legs execute
/// author logic inside the certifier process. This fence is the boundary between that logic
/// and the process holding the signing key. Reflection, I/O, process, environment, and
/// assembly-loading imports are refusals, not warnings.</para>
///
/// <para>The inventory blob is hashed into the certificate, so widening the fence is a new
/// judge rather than a silent change.</para>
/// </summary>
public static class IlImportFence
{
    /// <summary>Canonical inventory recorded as the <c>il-import-fence</c> input.</summary>
    public const string InventoryBlob =
        "mode=allowlist;v2;"
        + "allow-ns=System|System.Collections|System.Collections.Generic|System.Collections.ObjectModel|"
        + "System.Linq|System.Text|System.Globalization|System.Numerics|System.Threading|"
        + "System.Threading.Tasks|System.Runtime.CompilerServices|System.Runtime.ExceptionServices|"
        + "Ashlar.Core.Domain.Bricks|Ashlar.Core.Domain.Execution;"
        + "deny-type=System.Environment|System.AppDomain|System.AppContext|System.Activator|System.GC|"
        + "System.Console|System.OperatingSystem|System.Delegate|System.MulticastDelegate|System.Uri;"
        + "deny-member=System.Type::*(except GetTypeFromHandle);"
        + "deny-iface=Ashlar.Core.Domain.Execution.IExecutionContext";

    /// <summary>
    /// Namespaces a deterministic brick may import from. Exact match only: a child
    /// namespace must be listed in its own right, so <c>System.Reflection</c>,
    /// <c>System.IO</c>, <c>System.Diagnostics</c>, <c>System.Net</c>,
    /// <c>System.Runtime.InteropServices</c>, <c>System.Runtime.Loader</c> and
    /// <c>System.Linq.Expressions</c> are outside the fence without being enumerated.
    /// </summary>
    private static readonly HashSet<string> AllowedNamespaces = new(StringComparer.Ordinal)
    {
        "System",
        "System.Collections",
        "System.Collections.Generic",
        "System.Collections.ObjectModel",
        "System.Linq",
        "System.Text",
        "System.Globalization",
        "System.Numerics",
        "System.Threading",
        "System.Threading.Tasks",
        // The compiler emits async state machines and interpolated-string handlers against
        // these; a brick with an `async` method or a `$"…"` literal cannot compile without them.
        "System.Runtime.CompilerServices",
        "System.Runtime.ExceptionServices",
        "Ashlar.Core.Domain.Bricks",
        "Ashlar.Core.Domain.Execution",
    };

    /// <summary>
    /// Types inside an allowed namespace that are still refused. These are the ambient
    /// authority handles that happen to live in <c>System</c> — reading the environment,
    /// enumerating loaded assemblies, constructing types by name, or building delegates
    /// from metadata would each reach past the fence from inside it.
    /// </summary>
    private static readonly HashSet<string> DeniedTypes = new(StringComparer.Ordinal)
    {
        "System.Environment",
        "System.AppDomain",
        "System.AppContext",
        "System.Activator",
        "System.GC",
        "System.Console",
        "System.OperatingSystem",
        "System.Delegate",
        "System.MulticastDelegate",
    };

    /// <summary>
    /// <c>typeof(T)</c> lowers to <c>ldtoken</c> plus this call, which is inert on its own.
    /// Every other <c>System.Type</c> member is reflection and is refused.
    /// </summary>
    private const string TypeFromHandle = "GetTypeFromHandle";

    private const string ExecutionContextInterface = "Ashlar.Core.Domain.Execution.IExecutionContext";

    /// <summary>Signed input naming this fence inventory.</summary>
    public static CertificationInput ToInput() => new()
    {
        Kind = CertificationInputKinds.IlImportFence,
        Id = "allowlist-v2",
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
        // CertAuditContext is injected by CandidateSourceWrapper: the certifier's own
        // deterministic context, not an author-supplied one.
        if (type.Name != "CertAuditContext" && ImplementsExecutionContext(type))
        {
            throw new InvalidOperationException(
                $"il-import fence: type '{type.FullName}' implements {ExecutionContextInterface}. "
                + "The certifier supplies the execution context; an author-supplied one is refused.");
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
            if (instruction.OpCode != OpCodes.Call
                && instruction.OpCode != OpCodes.Callvirt
                && instruction.OpCode != OpCodes.Newobj
                && instruction.OpCode != OpCodes.Ldftn
                && instruction.OpCode != OpCodes.Ldvirtftn)
            {
                continue;
            }

            if (instruction.Operand is not MemberReference member)
                continue;

            var declaring = member.DeclaringType;
            if (declaring is null)
                continue;

            Verify(method, declaring, member.Name);
        }
    }

    private static void Verify(MethodDefinition method, TypeReference declaring, string memberName)
    {
        // Generic instantiations and arrays wrap the type that actually carries the
        // authority (List<Process>, Process[]); unwrap before judging.
        var root = Root(declaring);

        // A call into the emitted assembly's own types is judged by the rest of the fence
        // when those types are themselves walked.
        if (root.Scope is ModuleDefinition)
            return;

        var ns = root.Namespace ?? string.Empty;
        var fullName = string.IsNullOrEmpty(ns) ? root.Name : ns + "." + root.Name;

        if (!AllowedNamespaces.Contains(ns))
        {
            throw new InvalidOperationException(
                $"il-import fence: '{Describe(method)}' imports '{fullName}', whose namespace "
                + $"'{(ns.Length == 0 ? "<global>" : ns)}' is outside the brick API allowlist. "
                + "A certified brick may use primitives, collections, LINQ, text, globalization, "
                + "tasks and the Ashlar brick contracts; reflection, I/O, process, environment "
                + "and assembly loading are refused.");
        }

        if (DeniedTypes.Contains(fullName))
        {
            throw new InvalidOperationException(
                $"il-import fence: '{Describe(method)}' imports '{fullName}', which is ambient "
                + "authority (environment, assembly enumeration, activation or delegate "
                + "construction) and is refused inside a certified brick.");
        }

        if (string.Equals(fullName, "System.Type", StringComparison.Ordinal)
            && !string.Equals(memberName, TypeFromHandle, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"il-import fence: '{Describe(method)}' calls 'System.Type::{memberName}'. "
                + "Only typeof(...) is permitted; reflecting over types is refused.");
        }
    }

    private static TypeReference Root(TypeReference type)
    {
        var current = type;
        while (true)
        {
            switch (current)
            {
                case GenericInstanceType generic:
                    current = generic.ElementType;
                    continue;
                case TypeSpecification specification:
                    current = specification.ElementType;
                    continue;
                default:
                    return current;
            }
        }
    }

    private static string Describe(MethodDefinition method) =>
        method.DeclaringType.FullName + "::" + method.Name;
}
