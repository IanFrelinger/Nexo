using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Commands.NL
{
    public partial class BuildCommandFactory
    {
        private readonly ILogger _logger;
        public BuildCommandFactory(ILogger logger) { _logger = logger; }

        public Command CreateBuildCommand()
        {
            var cmd = new Command("run", "Execute a natural-language build");
            var description = new Argument<string>("description", "What you want to build");
            var platform = new Option<string>("--platform", () => "web", "Target platform");
            var output = new Option<string>("--output", () => "./generated-app", "Output directory");
            var install = new Option<bool>("--install", () => true, "Install dependencies");
            var run = new Option<bool>("--run", () => false, "Run after build");

            cmd.AddArgument(description);
            cmd.AddOption(platform);
            cmd.AddOption(output);
            cmd.AddOption(install);
            cmd.AddOption(run);

            cmd.SetHandler(async (string d, string p, string o, bool i, bool r, InvocationContext _) =>
            {
                await HandleBuildAsync(d, p, o, i, r);
            }, description, platform, output, install, run);

            return cmd;
        }

        private async Task HandleBuildAsync(string description, string platform, string output, bool install, bool run)
        {
            _logger.LogInformation("NL Build: description={Description}, platform={Platform}, output={Output}, install={Install}, run={Run}",
                description, platform, output, install, run);
            await Task.CompletedTask;
        }
    }
}
