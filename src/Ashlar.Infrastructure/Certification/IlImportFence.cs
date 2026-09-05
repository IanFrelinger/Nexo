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
/// read the certifier's own HMAC signing key. A later pass found the remaining holes
/// in that allowlist: P/Invoke methods have no IL body so a type-walk that skipped
/// <c>!HasBody</c> never saw <c>DllImport</c>, <c>calli</c> never appeared in the
/// inspected opcode set, module initializers run at <c>Assembly.Load</c>, and
/// <c>ldtoken</c> plus unused signature types could name types the brick never
/// called. The fence now inspects metadata (signatures, base types, P/Invoke,
/// dangerous attributes) and the full interesting opcode set, not just calls.</para>
///
/// <para><b>Why it is load-bearing.</b> The witness, determinism, and mutation legs execute
/// author logic inside the certifier process. This fence is the boundary between that logic
/// and the process holding the signing key. Reflection, I/O, process, environment,
/// interop, and assembly-loading imports are refusals, not warnings.</para>
///
/// <para>The inventory blob is hashed into the certificate, so widening the fence is a new
/// judge rather than a silent change.</para>
/// </summary>
public static class IlImportFence
{
    /// <summary>Canonical inventory recorded as the <c>il-import-fence</c> input.</summary>
    public const string InventoryBlob =
        "mode=allowlist;v3;"
        + "allow-ns=System|System.Collections|System.Collections.Generic|System.Collections.ObjectModel|"
        + "System.Linq|System.Text|System.Globalization|System.Numerics|System.Threading|"
        + "System.Threading.Tasks|System.Runtime.CompilerServices|System.Runtime.ExceptionServices|"
        + "Ashlar.Core.Domain.Bricks|Ashlar.Core.Domain.Execution;"
        + "deny-type=System.Environment|System.AppDomain|System.AppContext|System.Activator|System.GC|"
        + "System.Console|System.OperatingSystem|System.Delegate|System.MulticastDelegate|"
        + "System.Runtime.CompilerServices.Unsafe|System.Threading.Thread|System.Threading.ThreadPool|"
        + "System.Threading.Timer|System.Threading.PeriodicTimer;"
        + "deny-attr=ModuleInitializerAttribute|DllImportAttribute|LibraryImportAttribute|UnmanagedCallersOnlyAttribute;"
        + "deny-pinvoke=true;deny-calli=true;deny-localloc=true;deny-async-void=true;"
        + "inspect=body|signature|fields|ldtoken|pinvoke|attributes|localloc;"
        + "deny-member=System.Type::*(except GetTypeFromHandle)|Task::Run|Task::Start|TaskFactory::StartNew|"
        + "CancellationTokenSource::CancelAfter;"
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
    /// from metadata would each reach past the fence from inside it. <c>Unsafe</c> is
    /// the pointer/memory escape hatch that lives in an otherwise-required compiler ns.
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
        "System.Runtime.CompilerServices.Unsafe",
        // Thread / ThreadPool / Timer are ambient certifier handles: fire-and-forget
        // work outlives the witness return. Awaited Task/async remain allowed.
        "System.Threading.Thread",
        "System.Threading.ThreadPool",
        "System.Threading.Timer",
        "System.Threading.PeriodicTimer",
    };

    /// <summary>
    /// Members inside an otherwise-allowed type that start work the witness does not
    /// await. <c>Task.Delay</c> / <c>FromResult</c> / <c>WhenAll</c> stay permitted.
    /// </summary>
    private static readonly HashSet<(string Type, string Member)> DeniedMembers = new()
    {
        ("System.Threading.Tasks.Task", "Run"),
        ("System.Threading.Tasks.Task", "Start"),
        ("System.Threading.Tasks.TaskFactory", "StartNew"),
        ("System.Threading.CancellationTokenSource", "CancelAfter"),
    };

    /// <summary>
    /// Runtime bases the compiler assigns to module-owned types (lambdas, enums).
    /// Calling <c>Delegate.CreateDelegate</c> is still refused; inheriting
    /// <c>MulticastDelegate</c> is not an escape.
    /// </summary>
    private static readonly HashSet<string> AllowedRuntimeBases = new(StringComparer.Ordinal)
    {
        "System.Object",
        "System.ValueType",
        "System.Enum",
        "System.Delegate",
        "System.MulticastDelegate",
    };

    /// <summary>
    /// Attributes that change load or calling convention. Compiler-emitted attributes
    /// in <c>System.Diagnostics</c> / <c>System.Reflection</c> are ignored; only this
    /// named set is refused, because a blanket attribute-namespace allowlist would
    /// reject every Release assembly for <c>DebuggableAttribute</c>.
    /// </summary>
    private static readonly HashSet<string> DeniedAttributes = new(StringComparer.Ordinal)
    {
        "System.Runtime.CompilerServices.ModuleInitializerAttribute",
        "System.Runtime.InteropServices.DllImportAttribute",
        "System.Runtime.InteropServices.LibraryImportAttribute",
        "System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute",
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
        Id = "allowlist-v3",
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

        var site = type.FullName;
        InspectAttributes(type, site);
        if (type.BaseType is not null)
            VerifyType(site, type.BaseType, skipDenied: AllowedRuntimeBases);
        foreach (var iface in type.Interfaces)
            VerifyType(site, iface.InterfaceType);

        foreach (var field in type.Fields)
        {
            var fieldSite = site + "::" + field.Name;
            InspectAttributes(field, fieldSite);
            VerifyType(fieldSite, field.FieldType);
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
        var site = Describe(method);
        InspectAttributes(method, site);
        if (method.MethodReturnType is not null)
        {
            InspectAttributes(method.MethodReturnType, site);
            VerifyType(site, method.ReturnType);
        }

        foreach (var parameter in method.Parameters)
        {
            InspectAttributes(parameter, site);
            VerifyType(site, parameter.ParameterType);
        }

        if (method.HasPInvokeInfo || method.IsPInvokeImpl)
        {
            var entry = method.HasPInvokeInfo
                ? method.PInvokeInfo.Module.Name + "!" + (method.PInvokeInfo.EntryPoint ?? method.Name)
                : method.Name;
            throw new InvalidOperationException(
                $"il-import fence: '{site}' is a P/Invoke to '{entry}'. "
                + "Native interop is outside the brick API allowlist and is refused.");
        }

        if (IsAsyncVoid(method))
        {
            throw new InvalidOperationException(
                $"il-import fence: '{site}' is async void. "
                + "Fire-and-forget async methods outlive the witness return and are refused.");
        }

        if (!method.HasBody)
            return;

        foreach (var instruction in method.Body.Instructions)
        {
            if (instruction.OpCode == OpCodes.Calli)
            {
                throw new InvalidOperationException(
                    $"il-import fence: '{site}' uses calli (a function pointer). "
                    + "Unmanaged calling conventions are refused inside a certified brick.");
            }

            if (instruction.OpCode == OpCodes.Localloc)
            {
                throw new InvalidOperationException(
                    $"il-import fence: '{site}' uses localloc (stackalloc). "
                    + "Unbounded stack allocation inside the certifier process is refused.");
            }

            if (instruction.OpCode == OpCodes.Ldtoken)
            {
                switch (instruction.Operand)
                {
                    case TypeReference type:
                        VerifyType(site, type);
                        break;
                    case MethodReference methodRef:
                        VerifyMember(site, methodRef.DeclaringType, methodRef.Name);
                        break;
                    case FieldReference fieldRef:
                        VerifyType(site, fieldRef.DeclaringType);
                        VerifyType(site, fieldRef.FieldType);
                        break;
                }

                continue;
            }

            if (instruction.OpCode == OpCodes.Call
                || instruction.OpCode == OpCodes.Callvirt
                || instruction.OpCode == OpCodes.Newobj
                || instruction.OpCode == OpCodes.Ldftn
                || instruction.OpCode == OpCodes.Ldvirtftn)
            {
                if (instruction.Operand is MemberReference member)
                    VerifyMember(site, member.DeclaringType, member.Name);
                continue;
            }

            if (instruction.OpCode == OpCodes.Ldfld
                || instruction.OpCode == OpCodes.Ldsfld
                || instruction.OpCode == OpCodes.Stfld
                || instruction.OpCode == OpCodes.Stsfld
                || instruction.OpCode == OpCodes.Ldflda
                || instruction.OpCode == OpCodes.Ldsflda)
            {
                if (instruction.Operand is FieldReference field)
                {
                    VerifyType(site, field.DeclaringType);
                    VerifyType(site, field.FieldType);
                }
            }
        }
    }

    private static void InspectAttributes(ICustomAttributeProvider provider, string site)
    {
        if (!provider.HasCustomAttributes)
            return;

        foreach (var attribute in provider.CustomAttributes)
        {
            var fullName = attribute.AttributeType.FullName;
            if (DeniedAttributes.Contains(fullName))
            {
                throw new InvalidOperationException(
                    $"il-import fence: '{site}' is marked [{fullName}], which changes load "
                    + "or calling convention and is refused inside a certified brick.");
            }
        }
    }

    private static void VerifyMember(string site, TypeReference? declaring, string memberName)
    {
        if (declaring is null)
            return;

        VerifyType(site, declaring);

        var root = Root(declaring);
        var ns = root.Namespace ?? string.Empty;
        var fullName = string.IsNullOrEmpty(ns) ? root.Name : ns + "." + root.Name;
        if (string.Equals(fullName, "System.Type", StringComparison.Ordinal)
            && !string.Equals(memberName, TypeFromHandle, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"il-import fence: '{site}' calls 'System.Type::{memberName}'. "
                + "Only typeof(...) is permitted; reflecting over types is refused.");
        }

        if (DeniedMembers.Contains((fullName, memberName)))
        {
            throw new InvalidOperationException(
                $"il-import fence: '{site}' calls '{fullName}::{memberName}'. "
                + "Fire-and-forget scheduling inside the certifier process is refused.");
        }
    }

    private static bool IsAsyncVoid(MethodDefinition method)
    {
        if (method.ReturnType.MetadataType != MetadataType.Void)
            return false;
        foreach (var attribute in method.CustomAttributes)
        {
            if (string.Equals(
                    attribute.AttributeType.Name,
                    "AsyncStateMachineAttribute",
                    StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void VerifyType(string site, TypeReference type, HashSet<string>? skipDenied = null)
    {
        switch (type)
        {
            case GenericInstanceType generic:
                VerifyBareType(site, generic.ElementType, skipDenied);
                foreach (var argument in generic.GenericArguments)
                    VerifyType(site, argument, skipDenied);
                return;
            case TypeSpecification specification:
                VerifyType(site, specification.ElementType, skipDenied);
                return;
            default:
                VerifyBareType(site, type, skipDenied);
                return;
        }
    }

    private static void VerifyBareType(string site, TypeReference type, HashSet<string>? skipDenied)
    {
        // A reference into the emitted assembly's own types is judged when those
        // types are themselves walked.
        if (type.Scope is ModuleDefinition)
            return;

        var ns = type.Namespace ?? string.Empty;
        var fullName = string.IsNullOrEmpty(ns) ? type.Name : ns + "." + type.Name;

        if (!AllowedNamespaces.Contains(ns))
        {
            throw new InvalidOperationException(
                $"il-import fence: '{site}' imports '{fullName}', whose namespace "
                + $"'{(ns.Length == 0 ? "<global>" : ns)}' is outside the brick API allowlist. "
                + "A certified brick may use primitives, collections, LINQ, text, globalization, "
                + "tasks and the Ashlar brick contracts; reflection, I/O, process, environment "
                + "and assembly loading are refused.");
        }

        if (DeniedTypes.Contains(fullName) && (skipDenied is null || !skipDenied.Contains(fullName)))
        {
            throw new InvalidOperationException(
                $"il-import fence: '{site}' imports '{fullName}', which is ambient "
                + "authority (environment, assembly enumeration, activation, delegate "
                + "construction or unsafe memory) and is refused inside a certified brick.");
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
