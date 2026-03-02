using Mono.Cecil;
using Mono.Cecil.Cil;
using Nexo.Abstractions;
using System.Text.Json;

namespace Nexo.Tools.Assembly;

/// <summary>
/// Tool for security scanning .NET assemblies using Mono.Cecil.
/// Scans for dangerous API calls (Assembly.Load, Process.Start, deserialization, etc.).
/// </summary>
public sealed class AssemblySecurityScanTool : ITool
{
    public string Id => "assembly.security_scan";
    public ToolSchema Schema => new(Id, "Scan .NET assembly for security vulnerabilities", """
    {"type":"object","required":["path"],"properties":{"path":{"type":"string","description":"Path to .NET assembly"}}}
    """);

    private sealed record Args(string path);

    private static readonly HashSet<(string TypeName, string MethodName)> DangerousMethods = new(
        new (string, string)[]
        {
            ("System.Reflection.Assembly", "Load"),
        ("System.Reflection.Assembly", "LoadFrom"),
        ("System.Reflection.Assembly", "LoadFile"),
        ("System.Diagnostics.Process", "Start"),
        ("System.Runtime.Serialization.Formatters.Binary.BinaryFormatter", "Deserialize"),
        ("System.Runtime.Serialization.NetDataContractSerializer", "Deserialize"),
        ("System.Runtime.Serialization.Formatters.Soap.SoapFormatter", "Deserialize"),
        ("System.IO.File", "Delete"),
        ("System.IO.File", "WriteAllText"),
        ("System.IO.File", "WriteAllBytes"),
        ("System.IO.Directory", "Delete"),
        ("System.Data.SqlClient.SqlCommand", "ExecuteNonQuery"),
        ("System.Data.SqlClient.SqlCommand", "ExecuteReader"),
        ("Microsoft.Data.SqlClient.SqlCommand", "ExecuteNonQuery"),
        ("Microsoft.Data.SqlClient.SqlCommand", "ExecuteReader"),
        },
        new DangerousMethodComparer());

    private sealed class DangerousMethodComparer : IEqualityComparer<(string TypeName, string MethodName)>
    {
        public bool Equals((string TypeName, string MethodName) x, (string TypeName, string MethodName) y) =>
            string.Equals(x.TypeName, y.TypeName, StringComparison.Ordinal) &&
            string.Equals(x.MethodName, y.MethodName, StringComparison.Ordinal);
        public int GetHashCode((string TypeName, string MethodName) obj) =>
            HashCode.Combine(obj.TypeName, obj.MethodName);
    }

    public Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<Args>(call.Arguments)!;
        ct.ThrowIfCancellationRequested();

        var findings = new List<object>();

        if (!File.Exists(args.path))
        {
            var errDelta = new ActionDelta(s.Tick, s.Tick + 1, new[] { $"security_scan:FAILED - file not found: {args.path}" });
            return Task.FromResult(new ToolResult(errDelta, new { Findings = findings, Count = 0, Error = $"Assembly not found: {args.path}" }));
        }

        try
        {
            using var module = ModuleDefinition.ReadModule(args.path, new ReaderParameters { InMemory = true });
            foreach (var type in module.Types)
            {
                if (type.Namespace == "<Module>") continue;
                foreach (var method in type.Methods)
                {
                    if (!method.HasBody) continue;
                    foreach (var instruction in method.Body.Instructions)
                    {
                        if (instruction.OpCode.Code != Code.Call && instruction.OpCode.Code != Code.Callvirt)
                            continue;
                        var operand = instruction.Operand as MethodReference;
                        if (operand == null) continue;
                        var declType = operand.DeclaringType?.FullName ?? "";
                        var methodName = operand.Name ?? "";
                        if (DangerousMethods.Contains((declType, methodName)))
                        {
                            findings.Add(new
                            {
                                Severity = "Medium",
                                Type = "DangerousApiCall",
                                DeclaringType = declType,
                                Method = methodName,
                                Location = $"{type.FullName}.{method.Name}",
                                Message = $"Calls {declType}.{methodName}"
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            findings.Add(new { Severity = "Medium", Type = "ScanError", Message = ex.Message });
        }

        var delta = new ActionDelta(s.Tick, s.Tick + 1, new[] { $"security_scan:{args.path} findings={findings.Count}" });
        var payload = new { Findings = findings, Count = findings.Count };
        return Task.FromResult(new ToolResult(delta, payload));
    }
}
