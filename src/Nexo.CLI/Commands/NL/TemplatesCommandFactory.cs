using System.CommandLine;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Commands.NL
{
    public class TemplatesCommandFactory
    {
        private readonly ILogger _logger;
        public TemplatesCommandFactory(ILogger logger) { _logger = logger; }

        public Command CreateTemplatesCommand()
        {
            var cmd = new Command("templates", "List available templates");
            cmd.SetHandler(() => _logger.LogInformation("Templates: web, mobile, desktop, api, game"));
            return cmd;
        }
    }
}
