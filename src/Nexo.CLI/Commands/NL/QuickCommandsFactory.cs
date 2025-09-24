using System;
using System.CommandLine;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Commands.NL
{
    public class QuickCommandsFactory
    {
        private readonly ILogger _logger;
        public QuickCommandsFactory(ILogger logger) { _logger = logger; }

        public Command CreateQuickRoot()
        {
            var quick = new Command("quick", "Quick build commands");
            quick.AddCommand(CreateQuick("web"));
            quick.AddCommand(CreateQuick("mobile"));
            quick.AddCommand(CreateQuick("api"));
            quick.AddCommand(CreateQuick("game"));
            return quick;
        }

        private Command CreateQuick(string platform)
        {
            var cmd = new Command(platform, $"Quick build for {platform}");
            cmd.SetHandler(() => _logger.LogInformation("Quick build: {Platform}", platform));
            return cmd;
        }
    }
}
