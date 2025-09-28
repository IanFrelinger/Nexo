using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System;
using System.Threading.Tasks;

namespace Nexo.CLI
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            
            // Configure services
            builder.Services.AddLogging(configure => configure.AddConsole());
            
            var host = builder.Build();
            
            // Create root command
            var rootCommand = new RootCommand("Nexo - Core AI Development Platform");
            
            // Add basic commands
            var versionCommand = new Command("version", "Show version information");
            versionCommand.SetHandler(() =>
            {
                Console.WriteLine("Nexo Core v1.0.0");
            });
            
            var helpCommand = new Command("help", "Show help information");
            helpCommand.SetHandler(() =>
            {
                Console.WriteLine("Nexo Core - AI Development Platform");
                Console.WriteLine("Available commands:");
                Console.WriteLine("  version - Show version information");
                Console.WriteLine("  help    - Show this help");
            });
            
            rootCommand.AddCommand(versionCommand);
            rootCommand.AddCommand(helpCommand);
            
            return await rootCommand.InvokeAsync(args);
        }
    }
}