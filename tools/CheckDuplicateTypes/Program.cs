using System.Reflection;
using System.Text;
var roots = new [] { "src" };
var asmPaths = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.dll", SearchOption.AllDirectories)
    .Where(p => p.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) && p.EndsWith($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}"))
    .Distinct().ToList();

var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
foreach (var p in asmPaths)
{
    try
    {
        var asm = Assembly.LoadFrom(p);
        foreach (var t in asm.GetExportedTypes())
        {
            var name = t.Name;
            if (!map.TryGetValue(name, out var list)) map[name] = list = new();
            var loc = $"{asm.GetName().Name}::{t.FullName}";
            if (!list.Contains(loc)) list.Add(loc);
        }
    }
    catch { /* skip broken loads */ }
}
var dups = map.Where(kv => kv.Value.Count > 1).ToList();
if (dups.Count > 0)
{
    var sb = new StringBuilder();
    sb.AppendLine("Duplicate public type names detected:");
    foreach (var d in dups)
    {
        sb.AppendLine($"  {d.Key}");
        foreach (var loc in d.Value) sb.AppendLine($"    - {loc}");
    }
    Console.Error.WriteLine(sb.ToString());
    Environment.Exit(2);
}
