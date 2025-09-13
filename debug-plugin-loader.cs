using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.Extensions;
using Nexo.Core.Domain.Interfaces;

// Create a simple console app to test the PluginLoader
var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<PluginLoader>();
var pluginLoader = new PluginLoader(logger);

var assemblyPath = Path.Combine(Directory.GetCurrentDirectory(), "test-plugins", "TestPlugin", "bin", "Debug", "net8.0", "TestPlugin.dll");

Console.WriteLine($"Looking for assembly at: {assemblyPath}");
Console.WriteLine($"File exists: {File.Exists(assemblyPath)}");

if (File.Exists(assemblyPath))
{
    try
    {
        var result = await pluginLoader.LoadPluginAsync(assemblyPath, "TestPlugin");
        Console.WriteLine($"Load result: Success={result.IsSuccess}, Plugin={result.Plugin?.Name}, Errors={result.ErrorCount}");
        
        if (result.HasErrors)
        {
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"Error: {error.Message} (Code: {error.Id})");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
}
