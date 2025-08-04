using System;
using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.WebOnly
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;
                services.AddNexoWebOnlyCliServices(configuration);
            })
            .Build();

            var rootCommand = new RootCommand("Nexo Web-Only CLI - Web Code Generation Platform");
            
            // Version command
            var versionCommand = new Command("version", "Display version information");
            versionCommand.SetHandler(() =>
            {
                Console.WriteLine("Nexo Web-Only CLI v1.0.0");
                Console.WriteLine("Web Code Generation Platform");
                Console.WriteLine("Features: React, Vue, Angular, Svelte, Next.js, Nuxt.js");
                Console.WriteLine("WebAssembly Optimization & Code Generation");
            });
            rootCommand.AddCommand(versionCommand);

            // Web commands
            var webCodeGenerator = host.Services.GetRequiredService<Nexo.Feature.Web.Interfaces.IWebCodeGenerator>();
            var wasmOptimizer = host.Services.GetRequiredService<Nexo.Feature.Web.Interfaces.IWebAssemblyOptimizer>();
            var generateWebCodeUseCase = host.Services.GetRequiredService<Nexo.Feature.Web.UseCases.GenerateWebCodeUseCase>();
            var webCommand = Nexo.CLI.Commands.WebCommands.CreateWebCommand(webCodeGenerator, wasmOptimizer, generateWebCodeUseCase, host.Services.GetRequiredService<ILogger<Program>>());
            rootCommand.AddCommand(webCommand);

            // Help command
            var helpCommand = new Command("help", "Show detailed help information");
            helpCommand.SetHandler(() =>
            {
                Console.WriteLine("Nexo Web-Only CLI Help");
                Console.WriteLine("=====================");
                Console.WriteLine();
                Console.WriteLine("Available Commands:");
                Console.WriteLine("  web generate    - Generate web components");
                Console.WriteLine("  web optimize    - Optimize WebAssembly code");
                Console.WriteLine("  web analyze     - Analyze web code performance");
                Console.WriteLine("  web list        - List supported frameworks");
                Console.WriteLine("  web validate    - Validate web code");
                Console.WriteLine("  version         - Show version information");
                Console.WriteLine("  help            - Show this help message");
                Console.WriteLine();
                Console.WriteLine("Examples:");
                Console.WriteLine("  dotnet run -- web generate --component-name MyComponent --framework React");
                Console.WriteLine("  dotnet run -- web optimize --source-code \"function test() {}\"");
                Console.WriteLine("  dotnet run -- web list");
            });
            rootCommand.AddCommand(helpCommand);

            return await rootCommand.InvokeAsync(args);
        }
    }
}
