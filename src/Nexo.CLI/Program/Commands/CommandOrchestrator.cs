using System;
using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.CLI.Commands;
using Nexo.CLI.Commands.Unity;
using Nexo.Infrastructure.Commands;

namespace Nexo.CLI.Program.Commands
{
    /// <summary>
    /// Orchestrates the creation and configuration of all CLI commands
    /// </summary>
    public partial class CommandOrchestrator
    {
        private readonly IServiceProvider _serviceProvider;

        public CommandOrchestrator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public RootCommand CreateRootCommand()
        {
            var rootCommand = new RootCommand("Nexo - AI-Enhanced Development Environment Orchestration Platform");

            // Add basic commands
            AddBasicCommands(rootCommand);

            // Add AI commands
            AddAICommands(rootCommand);

            // Add pipeline commands
            AddPipelineCommands(rootCommand);

            // Add development commands
            AddDevelopmentCommands(rootCommand);

            // Add project commands
            AddProjectCommands(rootCommand);

            // Add configuration commands
            AddConfigurationCommands(rootCommand);

            // Add testing commands
            AddTestingCommands(rootCommand);

            // Add web commands
            AddWebCommands(rootCommand);

            // Add feature factory commands
            AddFeatureFactoryCommands(rootCommand);

            // Add enhanced CLI commands
            AddEnhancedCLICommands(rootCommand);

            // Add Unity commands
            AddUnityCommands(rootCommand);

            // Add game development commands
            AddGameDevelopmentCommands(rootCommand);

            // Add adaptation commands
            AddAdaptationCommands(rootCommand);

            // Add agent management commands
            AddAgentManagementCommands(rootCommand);

            // Add iteration commands
            AddIterationCommands(rootCommand);

            // Add tool generation commands
            AddToolGenerationCommands(rootCommand);

            // Add maintenance commands
            AddMaintenanceCommands(rootCommand);

            // Add hardware commands
            AddHardwareCommands(rootCommand);

            // Add extension commands
            AddExtensionCommands(rootCommand);

            // Add chat and model commands
            AddChatAndModelCommands(rootCommand);

            // Add policy commands
            AddPolicyCommands(rootCommand);

            // Add verification commands
            AddVerificationCommands(rootCommand);

            rootCommand.Description = "Nexo CLI provides AI-enhanced development environment orchestration capabilities with interactive mode, real-time dashboards, intelligent suggestions, and Unity game development tools.";

            return rootCommand;
        }

        private void AddBasicCommands(RootCommand rootCommand)
        {
            var versionCommand = new Command("version", "Display version information");
            versionCommand.SetHandler(() =>
            {
                Console.WriteLine("Nexo CLI v1.0.0");
                Console.WriteLine("AI-Enhanced Development Environment Orchestration Platform");
            });
            rootCommand.AddCommand(versionCommand);
        }

        private void AddAICommands(RootCommand rootCommand)
        {
#if !EXCLUDE_AI
            var aiCommandHandler = new AICommandHandler(_serviceProvider);
            var aiCommand = aiCommandHandler.CreateAICommand();
            rootCommand.AddCommand(aiCommand);
#endif
        }

        private void AddPipelineCommands(RootCommand rootCommand)
        {
            var pipelineCommandHandler = new PipelineCommandHandler(_serviceProvider);
            var pipelineCommand = pipelineCommandHandler.CreatePipelineCommand();
            rootCommand.AddCommand(pipelineCommand);
        }

        private void AddDevelopmentCommands(RootCommand rootCommand)
        {
            using var scope = _serviceProvider.CreateScope();
            var developmentAccelerator = scope.ServiceProvider.GetRequiredService<IDevelopmentAccelerator>();
            var workflowExecutionService = scope.ServiceProvider.GetRequiredService<IWorkflowExecutionService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            var devCommand = DevelopmentCommands.CreateDevelopmentCommand(developmentAccelerator, workflowExecutionService, logger);
            rootCommand.AddCommand(devCommand);

            var interactiveCommand = InteractiveCommands.CreateInteractiveCommand(developmentAccelerator, logger);
            rootCommand.AddCommand(interactiveCommand);
        }

        private void AddProjectCommands(RootCommand rootCommand)
        {
            using var scope = _serviceProvider.CreateScope();
            var templateService = scope.ServiceProvider.GetRequiredService<ITemplateService>();
            var intelligentTemplateService = scope.ServiceProvider.GetRequiredService<IIntelligentTemplateService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            var enhancedProjectCommand = ProjectCommands.CreateProjectCommand(templateService, intelligentTemplateService, logger);
            rootCommand.AddCommand(enhancedProjectCommand);
        }

        private void AddConfigurationCommands(RootCommand rootCommand)
        {
            using var scope = _serviceProvider.CreateScope();
            var aiConfigurationService = scope.ServiceProvider.GetRequiredService<IAiConfigurationService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            var configCommand = ConfigurationCommands.CreateConfigurationCommand(aiConfigurationService, logger);
            rootCommand.AddCommand(configCommand);
        }

        private void AddTestingCommands(RootCommand rootCommand)
        {
            using var scope = _serviceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            var standaloneTestingCommand = StandaloneTestRunner.CreateStandaloneTestCommand(logger);
            rootCommand.AddCommand(standaloneTestingCommand);
        }

        private void AddWebCommands(RootCommand rootCommand)
        {
            using var scope = _serviceProvider.CreateScope();
            var webCodeGenerator = scope.ServiceProvider.GetRequiredService<Nexo.Feature.Web.Interfaces.IWebCodeGenerator>();
            var wasmOptimizer = scope.ServiceProvider.GetRequiredService<Nexo.Feature.Web.Interfaces.IWebAssemblyOptimizer>();
            var generateWebCodeUseCase = scope.ServiceProvider.GetRequiredService<Nexo.Feature.Web.UseCases.GenerateWebCodeUseCase>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            var webCommand = WebCommands.CreateWebCommand(webCodeGenerator, wasmOptimizer, generateWebCodeUseCase, logger);
            rootCommand.AddCommand(webCommand);
        }

        private void AddFeatureFactoryCommands(RootCommand rootCommand)
        {
            using var scope = _serviceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            var featureFactoryCommand = FeatureFactoryCommands.CreateFeatureFactoryCommand(scope.ServiceProvider, logger);
            rootCommand.AddCommand(featureFactoryCommand);
        }

        private void AddEnhancedCLICommands(RootCommand rootCommand)
        {
            using var scope = _serviceProvider.CreateScope();
            var interactiveCLI = scope.ServiceProvider.GetRequiredService<Nexo.CLI.Interactive.IInteractiveCLI>();
            var realTimeDashboard = scope.ServiceProvider.GetRequiredService<Nexo.CLI.Dashboard.IRealTimeDashboard>();
            var interactiveHelpSystem = scope.ServiceProvider.GetRequiredService<Nexo.CLI.Help.IInteractiveHelpSystem>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            var enhancedInteractiveCommand = EnhancedCLICommands.CreateEnhancedInteractiveCommand(interactiveCLI, realTimeDashboard, interactiveHelpSystem, logger);
            rootCommand.AddCommand(enhancedInteractiveCommand);

            var enhancedHelpCommand = EnhancedCLICommands.CreateEnhancedHelpCommand(interactiveHelpSystem, logger);
            rootCommand.AddCommand(enhancedHelpCommand);

            var enhancedDashboardCommand = EnhancedCLICommands.CreateEnhancedDashboardCommand(realTimeDashboard, logger);
            rootCommand.AddCommand(enhancedDashboardCommand);

            var enhancedStatusCommand = EnhancedCLICommands.CreateEnhancedStatusCommand(interactiveCLI, logger);
            rootCommand.AddCommand(enhancedStatusCommand);
        }

        private void AddUnityCommands(RootCommand rootCommand)
        {
            using var scope = _serviceProvider.CreateScope();
            var unityCommand = UnityCommands.CreateUnityCommand(scope.ServiceProvider);
            rootCommand.AddCommand(unityCommand);
        }

        private void AddGameDevelopmentCommands(RootCommand rootCommand)
        {
            using var scope = _serviceProvider.CreateScope();
            var gameCommand = GameDevelopmentCommands.CreateGameDevelopmentCommand(scope.ServiceProvider);
            rootCommand.AddCommand(gameCommand);
        }

        private void AddAdaptationCommands(RootCommand rootCommand)
        {
            using var scope = _serviceProvider.CreateScope();
            var adaptationCommand = AdaptationCommands.CreateAdaptationCommand(scope.ServiceProvider);
            rootCommand.AddCommand(adaptationCommand);
        }

        private void AddAgentManagementCommands(RootCommand rootCommand)
        {
            using var scope = _serviceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            var agentManagementCommands = new AgentManagementCommands(scope.ServiceProvider, logger);
            var agentCommand = new Command("agents", "Manage specialized AI agents");
            agentCommand.AddCommand(agentManagementCommands.CreateAgentListCommand());
            agentCommand.AddCommand(agentManagementCommands.CreateAgentAnalyzeCommand());
            agentCommand.AddCommand(agentManagementCommands.CreateAgentPerformanceCommand());
            agentCommand.AddCommand(agentManagementCommands.CreateAgentTestCommand());
            agentCommand.AddCommand(agentManagementCommands.CreateAgentRegistryCommand());
            rootCommand.AddCommand(agentCommand);
        }

        private void AddIterationCommands(RootCommand rootCommand)
        {
            using var scope = _serviceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            var iterationCommands = new IterationCommands(scope.ServiceProvider, logger);
            var iterationCommand = new Command("iteration", "Iteration strategy analysis and optimization");
            iterationCommand.AddCommand(iterationCommands.CreateIterationAnalyzeCommand());
            iterationCommand.AddCommand(iterationCommands.CreateIterationBenchmarkCommand());
            iterationCommand.AddCommand(iterationCommands.CreateIterationGenerateCommand());
            iterationCommand.AddCommand(iterationCommands.CreateIterationOptimizeCommand());
            iterationCommand.AddCommand(iterationCommands.CreateIterationRecommendationsCommand());
            rootCommand.AddCommand(iterationCommand);
        }

        private void AddToolGenerationCommands(RootCommand rootCommand)
        {
            using var scope = _serviceProvider.CreateScope();
            var toolCommand = ToolGenerationCommands.CreateToolGenerationCommand(scope.ServiceProvider);
            rootCommand.AddCommand(toolCommand);
        }

        private void AddMaintenanceCommands(RootCommand rootCommand)
        {
            using var scope = _serviceProvider.CreateScope();
            var maintenanceService = scope.ServiceProvider.GetRequiredService<IToolMaintenanceService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            var maintenanceCommand = new MaintenanceCommand(maintenanceService, logger);
            var maintenanceCommandRoot = new Command("maintenance", "Tool maintenance and lifecycle management");
            maintenanceCommandRoot.SetHandler(async (string[] args) =>
            {
                await maintenanceCommand.ExecuteAsync(args);
            });
            rootCommand.AddCommand(maintenanceCommandRoot);
        }

        private void AddHardwareCommands(RootCommand rootCommand)
        {
            using var scope = _serviceProvider.CreateScope();
            var hardwareChecker = scope.ServiceProvider.GetRequiredService<IHardwareRequirementsChecker>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            var hardwareCommand = new HardwareCommand(hardwareChecker, logger);
            var hardwareCommandRoot = new Command("hardware", "Hardware requirements and cloud fallback options");
            hardwareCommandRoot.SetHandler(async (string[] args) =>
            {
                await hardwareCommand.ExecuteAsync(args);
            });
            rootCommand.AddCommand(hardwareCommandRoot);
        }

        private void AddExtensionCommands(RootCommand rootCommand)
        {
            using var scope = _serviceProvider.CreateScope();
            var extensionCommand = ExtensionCommands.CreateExtensionCommand(scope.ServiceProvider);
            rootCommand.AddCommand(extensionCommand);
        }

        private void AddChatAndModelCommands(RootCommand rootCommand)
        {
            using var scope = _serviceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            var chatCommand = new ChatCommand(scope.ServiceProvider, logger, scope.ServiceProvider.GetRequiredService<IModelOrchestrator>());
            var chatCommandRoot = chatCommand.CreateChatCommand();
            rootCommand.AddCommand(chatCommandRoot);

            var modelCommand = new ModelCommand(scope.ServiceProvider, logger, scope.ServiceProvider.GetRequiredService<IModelOrchestrator>());
            var modelCommandRoot = modelCommand.CreateModelCommand();
            rootCommand.AddCommand(modelCommandRoot);
        }

        private void AddPolicyCommands(RootCommand rootCommand)
        {
            using var scope = _serviceProvider.CreateScope();
            var policyCommands = scope.ServiceProvider.GetRequiredService<PolicyCommands>();
            var policyCommandRoot = policyCommands.CreateCommand();
            rootCommand.AddCommand(policyCommandRoot);
        }

        private void AddVerificationCommands(RootCommand rootCommand)
        {
            using var scope = _serviceProvider.CreateScope();
            var verifyCommand = Commands.VerifyCommand.CreateVerifyCommand();
            rootCommand.AddCommand(verifyCommand);

            var specCommand = Commands.SpecValidationCommand.CreateSpecValidationCommand(scope.ServiceProvider);
            rootCommand.AddCommand(specCommand);
        }
    }
}
