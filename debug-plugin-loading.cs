using System;
using System.IO;
using System.Reflection;
using Nexo.Core.Domain.Interfaces;

// Test loading the plugin assembly directly
var assemblyPath = Path.Combine(Directory.GetCurrentDirectory(), "test-plugins", "TestPlugin", "bin", "Debug", "net8.0", "TestPlugin.dll");

Console.WriteLine($"Looking for assembly at: {assemblyPath}");
Console.WriteLine($"File exists: {File.Exists(assemblyPath)}");

if (File.Exists(assemblyPath))
{
    try
    {
        var assembly = Assembly.LoadFrom(assemblyPath);
        Console.WriteLine($"Assembly loaded successfully: {assembly.FullName}");
        
        var types = assembly.GetTypes();
        Console.WriteLine($"Found {types.Length} types:");
        foreach (var type in types)
        {
            Console.WriteLine($"  - {type.Name} (implements IPlugin: {typeof(IPlugin).IsAssignableFrom(type)})");
        }
        
        var pluginType = types.FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
        Console.WriteLine($"Plugin type found: {pluginType?.Name}");
        
        if (pluginType != null)
        {
            var plugin = (IPlugin)Activator.CreateInstance(pluginType);
            Console.WriteLine($"Plugin created: {plugin?.Name} v{plugin?.Version}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error loading assembly: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
}
