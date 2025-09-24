using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Template.Interfaces;

namespace Nexo.CLI.Commands.Project.Handlers
{
    public class ProjectInitHandler
    {
        private readonly ITemplateService _templateService;
        private readonly IIntelligentTemplateService _intelligentTemplateService;
        private readonly ILogger _logger;

        public ProjectInitHandler(
            ITemplateService templateService,
            IIntelligentTemplateService intelligentTemplateService,
            ILogger logger)
        {
            _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
            _intelligentTemplateService = intelligentTemplateService ?? throw new ArgumentNullException(nameof(intelligentTemplateService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Command CreateInitCommand()
        {
            var initCommand = new Command("init", "Initialize a new project");
            var initNameOption = new Option<string>("--name", "Project name") { IsRequired = true };
            var initTypeOption = new Option<string>("--type", "Project type (webapi, console, library, microservice)") { IsRequired = true };
            var initPathOption = new Option<string>("--path", "Project path") { IsRequired = false };
            var initTemplateOption = new Option<string>("--template", "Template to use") { IsRequired = false };
            var initAiOption = new Option<bool>("--ai", "Use AI-enhanced initialization") { IsRequired = false };
            
            initCommand.AddOption(initNameOption);
            initCommand.AddOption(initTypeOption);
            initCommand.AddOption(initPathOption);
            initCommand.AddOption(initTemplateOption);
            initCommand.AddOption(initAiOption);
            
            initCommand.SetHandler(async (name, type, path, template, ai) =>
            {
                await HandleInitAsync(name, type, path, template, ai);
            }, initNameOption, initTypeOption, initPathOption, initTemplateOption, initAiOption);
            
            return initCommand;
        }

        private async Task HandleInitAsync(string name, string type, string path, string template, bool ai)
        {
            try
            {
                _logger.LogInformation("Initializing project: {Name} of type {Type}", name, type);
                
                var projectPath = path ?? Path.Combine(Directory.GetCurrentDirectory(), name);
                
                if (ai)
                {
                    Console.WriteLine($"Using AI-enhanced initialization for {name}...");
                    await _intelligentTemplateService.GenerateProjectAsync(name, type, projectPath);
                }
                else
                {
                    Console.WriteLine($"Initializing {name} project...");
                    await _templateService.CreateProjectAsync(name, type, projectPath, template);
                }
                
                Console.WriteLine($"Project '{name}' initialized successfully at {projectPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing project");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
