using System;
using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Template.Interfaces;

namespace Nexo.CLI.Commands.Project.Handlers
{
    public partial class ProjectScaffoldHandler
    {
        private readonly ITemplateService _templateService;
        private readonly IIntelligentTemplateService _intelligentTemplateService;
        private readonly ILogger _logger;

        public ProjectScaffoldHandler(
            ITemplateService templateService,
            IIntelligentTemplateService intelligentTemplateService,
            ILogger logger)
        {
            _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
            _intelligentTemplateService = intelligentTemplateService ?? throw new ArgumentNullException(nameof(intelligentTemplateService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Command CreateScaffoldCommand()
        {
            var scaffoldCommand = new Command("scaffold", "Scaffold project components");
            var scaffoldTypeOption = new Option<string>("--type", "Component type (controller, service, model, test)") { IsRequired = true };
            var scaffoldNameOption = new Option<string>("--name", "Component name") { IsRequired = true };
            var scaffoldPathOption = new Option<string>("--path", "Output path") { IsRequired = false };
            var scaffoldAiOption = new Option<bool>("--ai", "Use AI-enhanced scaffolding") { IsRequired = false };
            
            scaffoldCommand.AddOption(scaffoldTypeOption);
            scaffoldCommand.AddOption(scaffoldNameOption);
            scaffoldCommand.AddOption(scaffoldPathOption);
            scaffoldCommand.AddOption(scaffoldAiOption);
            
            scaffoldCommand.SetHandler(async (type, name, path, ai) =>
            {
                await HandleScaffoldAsync(type, name, path, ai);
            }, scaffoldTypeOption, scaffoldNameOption, scaffoldPathOption, scaffoldAiOption);
            
            return scaffoldCommand;
        }

        private async Task HandleScaffoldAsync(string type, string name, string path, bool ai)
        {
            try
            {
                _logger.LogInformation("Scaffolding {Type} component: {Name}", type, name);
                
                if (ai)
                {
                    Console.WriteLine($"Using AI-enhanced scaffolding for {type} '{name}'...");
                    await _intelligentTemplateService.GenerateComponentAsync(type, name, path);
                }
                else
                {
                    Console.WriteLine($"Scaffolding {type} '{name}'...");
                    await _templateService.CreateComponentAsync(type, name, path);
                }
                
                Console.WriteLine($"Component '{name}' scaffolded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scaffolding component");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
