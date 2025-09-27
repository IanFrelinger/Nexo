using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Observability;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Models;
using Nexo.Core.Application.Services;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Services;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Services;
using Nexo.Feature.Analysis.Interfaces;
using Nexo.Feature.Analysis.Services;
using Nexo.Feature.Agent.Interfaces;
using Nexo.Feature.Agent.Services;
using Nexo.Feature.Template.Interfaces;
using Nexo.Feature.Template.Services;
using Nexo.Infrastructure.Services;
using Nexo.Infrastructure.Services.AI;
using Nexo.Infrastructure.Services.Caching;
using Nexo.Shared;
using Nexo.Shared.Models;
using Nexo.Shared.Services;
using Nexo.Shared.Interfaces;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Models;
using Nexo.Feature.Pipeline.Services;
using Nexo.Infrastructure.Services.Resource;
using Nexo.Shared.Interfaces.Resource;
using Nexo.Feature.Factory;
using Nexo.Feature.Unity;
using Nexo.Core.Application.Services.Adaptation;
using Nexo.Core.Extensions;
using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading;

namespace Nexo.CLI
{
    /// <summary>
    /// Extension methods for registering enhanced CLI services
    /// </summary>
    public static class EnhancedCLIServiceExtensions
    {
        /// <summary>
        /// Adds enhanced CLI services including interactive mode, dashboard, and help system
        /// </summary>
        public static IServiceCollection AddEnhancedCLIServices(this IServiceCollection services)
        {
            // Interactive CLI services
            services.AddTransient<Nexo.CLI.Interactive.IInteractiveCLI, Nexo.CLI.Interactive.InteractiveCLI>();
            services.AddTransient<Nexo.CLI.Interactive.ICommandSuggestionEngine, Nexo.CLI.Interactive.CommandSuggestionEngine>();
            services.AddTransient<Nexo.CLI.Interactive.ICLIStateManager, Nexo.CLI.Interactive.CLIStateManager>();
            
            // Dashboard services
            services.AddTransient<Nexo.CLI.Dashboard.IRealTimeDashboard, Nexo.CLI.Dashboard.RealTimeDashboard>();
            services.AddTransient<Nexo.CLI.Dashboard.IDashboardWidget, Nexo.CLI.Dashboard.PerformanceWidget>();
            services.AddTransient<Nexo.CLI.Dashboard.IDashboardWidget, Nexo.CLI.Dashboard.AdaptationWidget>();
            services.AddTransient<Nexo.CLI.Dashboard.IDashboardWidget, Nexo.CLI.Dashboard.ProjectStatusWidget>();
            services.AddTransient<Nexo.CLI.Dashboard.IDashboardWidget, Nexo.CLI.Dashboard.SystemHealthWidget>();
            
            // Progress tracking services
            services.AddTransient<Nexo.CLI.Progress.IProgressTracker, Nexo.CLI.Progress.ProgressTracker>();
            services.AddTransient<Nexo.CLI.Progress.IMultiStepProgressDisplay, Nexo.CLI.Progress.MultiStepProgressDisplay>();
            
            // Help system services
            services.AddTransient<Nexo.CLI.Help.IInteractiveHelpSystem, Nexo.CLI.Help.InteractiveHelpSystem>();
            services.AddTransient<Nexo.CLI.Help.IDocumentationGenerator, Nexo.CLI.Help.CommandDocumentationGenerator>();
            services.AddTransient<Nexo.CLI.Help.IExampleRepository, Nexo.CLI.Help.ExampleRepository>();
            
            return services;
        }
    }

    /// <summary>
    /// Extension methods for registering tool generation services
    /// </summary>
    public static class ToolGenerationServiceExtensions
    {
        /// <summary>
        /// Adds tool generation services including code generation, compilation, and persistence
        /// </summary>
        public static IServiceCollection AddToolGenerationServices(this IServiceCollection services)
        {
            // Core tool generation services
            services.AddTransient<Nexo.Core.Domain.Interfaces.ICodeGenerator, Nexo.Infrastructure.Generation.CodeGenerator>();
            services.AddTransient<Nexo.Core.Domain.Interfaces.ICompilationService, Nexo.Infrastructure.Compilation.RoslynCompilationService>();
            services.AddTransient<Nexo.Core.Domain.Interfaces.IToolRepository, Nexo.Infrastructure.Persistence.ToolRepository>();
            services.AddTransient<Nexo.Core.Domain.Interfaces.IToolEvolver, Nexo.Infrastructure.Evolution.ToolEvolver>();
            services.AddTransient<Nexo.Core.Domain.Interfaces.ICodeQualityAnalyzer, Nexo.Infrastructure.Quality.CodeQualityAnalyzer>();
            services.AddTransient<Nexo.Infrastructure.Safety.EnhancedSafetyValidator>();
            services.AddTransient<Nexo.Core.Domain.Interfaces.IGuidedGenerationService, Nexo.Infrastructure.GuidedGeneration.GuidedGenerationService>();
            services.AddTransient<Nexo.Core.Domain.Interfaces.IToolMaintenanceService, Nexo.Infrastructure.Maintenance.ToolMaintenanceService>();
            services.AddTransient<Nexo.Core.Domain.Interfaces.IHardwareRequirementsChecker, Nexo.Infrastructure.Hardware.HardwareRequirementsChecker>();
            
            // Orchestrator
            services.AddTransient<Nexo.Infrastructure.Orchestration.ToolGenerationOrchestrator>();
            
            // Plugin loader (use existing implementation)
            services.AddTransient<Nexo.Core.Application.Interfaces.IPluginLoader, Nexo.Core.Application.Services.Extensions.PluginLoader>();
            
            return services;
        }
    }

    /// <summary>
    /// Extension methods for registering policy engine services
    /// </summary>
    public static class PolicyEngineServiceExtensions
    {
        /// <summary>
        /// Adds policy engine services for safety and quality validation
        /// </summary>
        public static IServiceCollection AddPolicyEngineServices(this IServiceCollection services)
        {
            // Policy engine
            services.AddTransient<Nexo.Core.Domain.Interfaces.IPolicyEngine, Nexo.Infrastructure.Policy.PolicyEngine>();
            
            // Policy commands
            services.AddTransient<Nexo.CLI.Commands.PolicyCommands>();
            
            return services;
        }
    }
}
