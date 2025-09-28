using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Values;
using Nexo.Shared.Values;

namespace Nexo.Core.Application.Agents
{
    /// <summary>
    /// Factory for creating and managing agents
    /// </summary>
    public class AgentFactory : IAgentFactory
    {
        private readonly Dictionary<AgentType, Func<AgentId, AgentName, PlatformType, AgentConfiguration?, IAgent>> _agentCreators;
        private readonly Dictionary<string, AgentTemplate> _agentTemplates;

        public AgentFactory()
        {
            _agentCreators = new Dictionary<AgentType, Func<AgentId, AgentName, PlatformType, AgentConfiguration?, IAgent>>
            {
                [AgentType.CodeGeneration] = (id, name, platform, config) => 
                    new CodeGenerationAgent(id, name, platform, config?.FocusAreas, config?.Configuration),
                [AgentType.SecurityAnalysis] = (id, name, platform, config) => 
                    new SecurityAnalysisAgent(id, name, platform, config?.FocusAreas, config?.Configuration),
                [AgentType.DataProcessing] = (id, name, platform, config) => 
                    new DataProcessingAgent(id, name, platform, config?.FocusAreas, config?.Configuration),
                [AgentType.UserInterface] = (id, name, platform, config) => 
                    new UserInterfaceAgent(id, name, platform, config?.FocusAreas, config?.Configuration),
                [AgentType.Infrastructure] = (id, name, platform, config) => 
                    new InfrastructureAgent(id, name, platform, config?.FocusAreas, config?.Configuration),
                [AgentType.Testing] = (id, name, platform, config) => 
                    new TestingAgent(id, name, platform, config?.FocusAreas, config?.Configuration),
                [AgentType.Documentation] = (id, name, platform, config) => 
                    new DocumentationAgent(id, name, platform, config?.FocusAreas, config?.Configuration),
                [AgentType.Custom] = (id, name, platform, config) => 
                    new CustomAgent(id, name, platform, config?.FocusAreas, config?.Configuration)
            };

            _agentTemplates = new Dictionary<string, AgentTemplate>
            {
                ["full-stack-developer"] = new AgentTemplate(
                    "Full-Stack Developer",
                    "Complete full-stack development agent",
                    AgentType.CodeGeneration,
                    new[] { "Code Generation", "Code Analysis", "User Interface Design", "Data Processing" },
                    new[] { "Web Development", "Backend Development", "Frontend Development", "Database Design" },
                    new Dictionary<string, object>
                    {
                        ["maxConcurrentTasks"] = 5,
                        ["preferredLanguages"] = new[] { "C#", "JavaScript", "TypeScript", "Python" },
                        ["frameworks"] = new[] { "ASP.NET Core", "React", "Angular", "Vue.js" }
                    }),

                ["security-specialist"] = new AgentTemplate(
                    "Security Specialist",
                    "Security-focused development agent",
                    AgentType.SecurityAnalysis,
                    new[] { "Security Analysis", "Code Analysis", "Infrastructure" },
                    new[] { "Security", "Vulnerability Assessment", "Penetration Testing", "Compliance" },
                    new Dictionary<string, object>
                    {
                        ["maxConcurrentTasks"] = 3,
                        ["securityLevel"] = "High",
                        ["complianceFrameworks"] = new[] { "OWASP", "NIST", "ISO 27001" }
                    }),

                ["data-scientist"] = new AgentTemplate(
                    "Data Scientist",
                    "Data processing and analysis agent",
                    AgentType.DataProcessing,
                    new[] { "Data Processing", "Code Analysis", "Natural Language Processing" },
                    new[] { "Data Analysis", "Machine Learning", "Statistics", "Visualization" },
                    new Dictionary<string, object>
                    {
                        ["maxConcurrentTasks"] = 4,
                        ["preferredLanguages"] = new[] { "Python", "R", "SQL" },
                        ["libraries"] = new[] { "pandas", "numpy", "scikit-learn", "tensorflow" }
                    }),

                ["ui-designer"] = new AgentTemplate(
                    "UI Designer",
                    "User interface design agent",
                    AgentType.UserInterface,
                    new[] { "User Interface Design", "Code Generation", "Code Analysis" },
                    new[] { "UI Design", "UX Design", "Frontend Development", "Responsive Design" },
                    new Dictionary<string, object>
                    {
                        ["maxConcurrentTasks"] = 3,
                        ["designTools"] = new[] { "Figma", "Sketch", "Adobe XD" },
                        ["frontendFrameworks"] = new[] { "React", "Vue.js", "Angular", "Svelte" }
                    }),

                ["devops-engineer"] = new AgentTemplate(
                    "DevOps Engineer",
                    "Infrastructure and deployment agent",
                    AgentType.Infrastructure,
                    new[] { "Cross-Platform Deployment", "Infrastructure", "Code Analysis" },
                    new[] { "DevOps", "CI/CD", "Containerization", "Cloud Computing" },
                    new Dictionary<string, object>
                    {
                        ["maxConcurrentTasks"] = 4,
                        ["cloudProviders"] = new[] { "AWS", "Azure", "GCP" },
                        ["containerization"] = new[] { "Docker", "Kubernetes", "Podman" }
                    })
            };
        }

        public async Task<IAgent> CreateAgentAsync(AgentType agentType, AgentId id, AgentName name, PlatformType platform)
        {
            return await CreateAgentAsync(agentType, id, name, platform, null);
        }

        public async Task<IAgent> CreateAgentAsync(AgentType agentType, AgentId id, AgentName name, PlatformType platform, AgentConfiguration? configuration)
        {
            if (!_agentCreators.TryGetValue(agentType, out var creator))
                throw new ArgumentException($"Unknown agent type: {agentType}");

            var agent = creator(id, name, platform, configuration);
            return await Task.FromResult(agent);
        }

        public async Task<IAgent> CreateAgentFromTemplateAsync(AgentTemplate template, AgentId id, AgentName name, PlatformType platform)
        {
            var configuration = new AgentConfiguration(
                template.FocusAreas,
                template.Capabilities,
                template.Configuration);

            return await CreateAgentAsync(template.AgentType, id, name, platform, configuration);
        }

        public async Task<IAgent> CloneAgentAsync(IAgent sourceAgent, AgentId newId, AgentName newName)
        {
            var state = sourceAgent.GetState();
            var newState = new AgentState(
                newId,
                newName,
                state.Status,
                state.Platform,
                state.Capabilities,
                state.FocusAreas,
                state.Configuration.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                state.CreatedAt,
                state.LastActivatedAt,
                state.LastDeactivatedAt,
                state.FailureReason,
                state.RecentExperiences,
                state.RuntimeData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                state.ActiveTasks,
                state.PendingMessages,
                state.LastUpdatedAt,
                state.Version);

            return await CreateAgentFromStateAsync(newState);
        }

        public async Task<IAgent> CreateAgentFromStateAsync(AgentState state)
        {
            // Determine agent type based on capabilities
            var agentType = DetermineAgentTypeFromCapabilities(state.Capabilities);
            
            var configuration = new AgentConfiguration(
                state.FocusAreas,
                state.Capabilities.Select(c => c.Name).ToList(),
                state.Configuration.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

            var agent = await CreateAgentAsync(agentType, state.Id, state.Name, state.Platform, configuration);
            
            // Restore agent state
            await agent.RestoreAsync(state);
            
            return agent;
        }

        public Task<IReadOnlyList<AgentType>> GetAvailableAgentTypesAsync()
        {
            var agentTypes = Enum.GetValues<AgentType>().ToList().AsReadOnly();
            return Task.FromResult(agentTypes);
        }

        public Task<IReadOnlyList<AgentTemplate>> GetAgentTemplatesAsync()
        {
            var templates = _agentTemplates.Values.ToList().AsReadOnly();
            return Task.FromResult(templates);
        }

        public async Task<AgentResult> ValidateAgentConfigurationAsync(AgentConfiguration configuration)
        {
            if (configuration == null)
                return AgentResult.Failure("Configuration cannot be null", operationType: "ValidateAgentConfiguration");

            var errors = new List<string>();
            var warnings = new List<string>();

            // Validate focus areas
            if (configuration.FocusAreas == null || !configuration.FocusAreas.Any())
            {
                warnings.Add("No focus areas specified");
            }

            // Validate capabilities
            if (configuration.Capabilities == null || !configuration.Capabilities.Any())
            {
                errors.Add("At least one capability must be specified");
            }

            // Validate configuration values
            if (configuration.Configuration != null)
            {
                foreach (var kvp in configuration.Configuration)
                {
                    if (string.IsNullOrEmpty(kvp.Key))
                    {
                        errors.Add("Configuration key cannot be null or empty");
                    }
                }
            }

            if (errors.Any())
            {
                return AgentResult.Failure(
                    $"Configuration validation failed: {string.Join(", ", errors)}",
                    metadata: new Dictionary<string, object>
                    {
                        ["errors"] = errors,
                        ["warnings"] = warnings
                    },
                    operationType: "ValidateAgentConfiguration");
            }

            return AgentResult.SuccessWithWarnings(
                "Configuration is valid",
                warnings,
                metadata: new Dictionary<string, object>
                {
                    ["warnings"] = warnings
                },
                operationType: "ValidateAgentConfiguration");
        }

        public async Task<AgentCreationOptions> GetAgentCreationOptionsAsync(PlatformType platform)
        {
            var availableTypes = await GetAvailableAgentTypesAsync();
            var availableTemplates = await GetAgentTemplatesAsync();
            
            var platformSpecificTypes = availableTypes.Where(type => IsAgentTypeSupportedOnPlatform(type, platform)).ToList();
            var platformSpecificTemplates = availableTemplates.Where(template => IsTemplateSupportedOnPlatform(template, platform)).ToList();

            return new AgentCreationOptions(
                platformSpecificTypes,
                platformSpecificTemplates,
                GetDefaultConfigurationForPlatform(platform),
                GetRecommendedCapabilitiesForPlatform(platform));
        }

        private AgentType DetermineAgentTypeFromCapabilities(IEnumerable<AgentCapability> capabilities)
        {
            var capabilityNames = capabilities.Select(c => c.Name.ToLowerInvariant()).ToHashSet();

            if (capabilityNames.Contains("code generation"))
                return AgentType.CodeGeneration;
            if (capabilityNames.Contains("security analysis"))
                return AgentType.SecurityAnalysis;
            if (capabilityNames.Contains("data processing"))
                return AgentType.DataProcessing;
            if (capabilityNames.Contains("user interface design"))
                return AgentType.UserInterface;
            if (capabilityNames.Contains("cross-platform deployment"))
                return AgentType.Infrastructure;
            if (capabilityNames.Contains("testing"))
                return AgentType.Testing;
            if (capabilityNames.Contains("documentation"))
                return AgentType.Documentation;

            return AgentType.Custom;
        }

        private bool IsAgentTypeSupportedOnPlatform(AgentType agentType, PlatformType platform)
        {
            // All agent types are supported on all platforms in this implementation
            return true;
        }

        private bool IsTemplateSupportedOnPlatform(AgentTemplate template, PlatformType platform)
        {
            // All templates are supported on all platforms in this implementation
            return true;
        }

        private AgentConfiguration GetDefaultConfigurationForPlatform(PlatformType platform)
        {
            var defaultConfig = new Dictionary<string, object>
            {
                ["maxConcurrentTasks"] = 3,
                ["timeout"] = TimeSpan.FromMinutes(30),
                ["retryCount"] = 3
            };

            // Add platform-specific defaults
            switch (platform.Name)
            {
                case "Web":
                    defaultConfig["supportedFrameworks"] = new[] { "React", "Angular", "Vue.js" };
                    break;
                case "Mobile":
                    defaultConfig["supportedFrameworks"] = new[] { "React Native", "Flutter", "Xamarin" };
                    break;
                case "Desktop":
                    defaultConfig["supportedFrameworks"] = new[] { "WPF", "WinUI", "Electron" };
                    break;
            }

            return new AgentConfiguration(
                new List<string> { "General Development" },
                new List<string> { "Code Generation", "Code Analysis" },
                defaultConfig);
        }

        private IReadOnlyList<string> GetRecommendedCapabilitiesForPlatform(PlatformType platform)
        {
            var capabilities = new List<string> { "Code Generation", "Code Analysis" };

            switch (platform.Name)
            {
                case "Web":
                    capabilities.AddRange(new[] { "User Interface Design", "Cross-Platform Deployment" });
                    break;
                case "Mobile":
                    capabilities.AddRange(new[] { "User Interface Design", "Cross-Platform Deployment" });
                    break;
                case "Desktop":
                    capabilities.AddRange(new[] { "User Interface Design", "Infrastructure" });
                    break;
                case "Cloud":
                    capabilities.AddRange(new[] { "Infrastructure", "Cross-Platform Deployment" });
                    break;
            }

            return capabilities.AsReadOnly();
        }
    }

    // Supporting classes
    public class AgentTemplate
    {
        public string Name { get; }
        public string Description { get; }
        public AgentType AgentType { get; }
        public IReadOnlyList<string> Capabilities { get; }
        public IReadOnlyList<string> FocusAreas { get; }
        public IReadOnlyDictionary<string, object> Configuration { get; }

        public AgentTemplate(
            string name,
            string description,
            AgentType agentType,
            IEnumerable<string> capabilities,
            IEnumerable<string> focusAreas,
            IDictionary<string, object> configuration)
        {
            Name = name;
            Description = description;
            AgentType = agentType;
            Capabilities = capabilities.ToList().AsReadOnly();
            FocusAreas = focusAreas.ToList().AsReadOnly();
            Configuration = configuration.ToDictionary(kvp => kvp.Key, kvp => kvp.Value).AsReadOnly();
        }
    }

    public class AgentConfiguration
    {
        public IReadOnlyList<string> FocusAreas { get; }
        public IReadOnlyList<string> Capabilities { get; }
        public IReadOnlyDictionary<string, object> Configuration { get; }

        public AgentConfiguration(
            IEnumerable<string>? focusAreas = null,
            IEnumerable<string>? capabilities = null,
            IDictionary<string, object>? configuration = null)
        {
            FocusAreas = (focusAreas?.ToList() ?? new List<string>()).AsReadOnly();
            Capabilities = (capabilities?.ToList() ?? new List<string>()).AsReadOnly();
            Configuration = (configuration?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>()).AsReadOnly();
        }
    }

    public class AgentCreationOptions
    {
        public IReadOnlyList<AgentType> AvailableAgentTypes { get; }
        public IReadOnlyList<AgentTemplate> AvailableTemplates { get; }
        public AgentConfiguration DefaultConfiguration { get; }
        public IReadOnlyList<string> RecommendedCapabilities { get; }

        public AgentCreationOptions(
            IReadOnlyList<AgentType> availableAgentTypes,
            IReadOnlyList<AgentTemplate> availableTemplates,
            AgentConfiguration defaultConfiguration,
            IReadOnlyList<string> recommendedCapabilities)
        {
            AvailableAgentTypes = availableAgentTypes;
            AvailableTemplates = availableTemplates;
            DefaultConfiguration = defaultConfiguration;
            RecommendedCapabilities = recommendedCapabilities;
        }
    }

    public enum AgentType
    {
        CodeGeneration,
        SecurityAnalysis,
        DataProcessing,
        UserInterface,
        Infrastructure,
        Testing,
        Documentation,
        Custom
    }

    // Placeholder agent implementations
    public class DataProcessingAgent : CrossPlatformAgent
    {
        public DataProcessingAgent(AgentId id, AgentName name, PlatformType platform, IEnumerable<string>? focusAreas = null, IDictionary<string, object>? configuration = null)
            : base(id, name, platform, GetDefaultCapabilities(), focusAreas, configuration) { }

        private static IEnumerable<AgentCapability> GetDefaultCapabilities()
        {
            return new[] { AgentCapability.DataProcessing, AgentCapability.NaturalLanguageProcessing };
        }

        protected override Task OnInitializeAsync(AgentContext context) => Task.CompletedTask;
        protected override Task<AgentResult> OnExecuteAsync(AgentTask task) => Task.FromResult(AgentResult.Success("Data processing completed"));
        protected override Task<AgentResult> OnCommunicateAsync(IAgent targetAgent, AgentMessage message) => Task.FromResult(AgentResult.Success("Communication handled"));
        protected override Task<AgentResult> OnLearnAsync(AgentExperience experience) => Task.FromResult(AgentResult.Success("Learning completed"));
        protected override Task OnShutdownAsync() => Task.CompletedTask;
        protected override Task OnRestoreAsync(AgentState state) => Task.CompletedTask;
    }

    public class UserInterfaceAgent : CrossPlatformAgent
    {
        public UserInterfaceAgent(AgentId id, AgentName name, PlatformType platform, IEnumerable<string>? focusAreas = null, IDictionary<string, object>? configuration = null)
            : base(id, name, platform, GetDefaultCapabilities(), focusAreas, configuration) { }

        private static IEnumerable<AgentCapability> GetDefaultCapabilities()
        {
            return new[] { AgentCapability.UserInterfaceDesign, AgentCapability.CodeGeneration };
        }

        protected override Task OnInitializeAsync(AgentContext context) => Task.CompletedTask;
        protected override Task<AgentResult> OnExecuteAsync(AgentTask task) => Task.FromResult(AgentResult.Success("UI task completed"));
        protected override Task<AgentResult> OnCommunicateAsync(IAgent targetAgent, AgentMessage message) => Task.FromResult(AgentResult.Success("Communication handled"));
        protected override Task<AgentResult> OnLearnAsync(AgentExperience experience) => Task.FromResult(AgentResult.Success("Learning completed"));
        protected override Task OnShutdownAsync() => Task.CompletedTask;
        protected override Task OnRestoreAsync(AgentState state) => Task.CompletedTask;
    }

    public class InfrastructureAgent : CrossPlatformAgent
    {
        public InfrastructureAgent(AgentId id, AgentName name, PlatformType platform, IEnumerable<string>? focusAreas = null, IDictionary<string, object>? configuration = null)
            : base(id, name, platform, GetDefaultCapabilities(), focusAreas, configuration) { }

        private static IEnumerable<AgentCapability> GetDefaultCapabilities()
        {
            return new[] { AgentCapability.CrossPlatformDeployment, AgentCapability.Infrastructure };
        }

        protected override Task OnInitializeAsync(AgentContext context) => Task.CompletedTask;
        protected override Task<AgentResult> OnExecuteAsync(AgentTask task) => Task.FromResult(AgentResult.Success("Infrastructure task completed"));
        protected override Task<AgentResult> OnCommunicateAsync(IAgent targetAgent, AgentMessage message) => Task.FromResult(AgentResult.Success("Communication handled"));
        protected override Task<AgentResult> OnLearnAsync(AgentExperience experience) => Task.FromResult(AgentResult.Success("Learning completed"));
        protected override Task OnShutdownAsync() => Task.CompletedTask;
        protected override Task OnRestoreAsync(AgentState state) => Task.CompletedTask;
    }

    public class TestingAgent : CrossPlatformAgent
    {
        public TestingAgent(AgentId id, AgentName name, PlatformType platform, IEnumerable<string>? focusAreas = null, IDictionary<string, object>? configuration = null)
            : base(id, name, platform, GetDefaultCapabilities(), focusAreas, configuration) { }

        private static IEnumerable<AgentCapability> GetDefaultCapabilities()
        {
            return new[] { AgentCapability.CodeAnalysis, AgentCapability.Testing };
        }

        protected override Task OnInitializeAsync(AgentContext context) => Task.CompletedTask;
        protected override Task<AgentResult> OnExecuteAsync(AgentTask task) => Task.FromResult(AgentResult.Success("Testing task completed"));
        protected override Task<AgentResult> OnCommunicateAsync(IAgent targetAgent, AgentMessage message) => Task.FromResult(AgentResult.Success("Communication handled"));
        protected override Task<AgentResult> OnLearnAsync(AgentExperience experience) => Task.FromResult(AgentResult.Success("Learning completed"));
        protected override Task OnShutdownAsync() => Task.CompletedTask;
        protected override Task OnRestoreAsync(AgentState state) => Task.CompletedTask;
    }

    public class DocumentationAgent : CrossPlatformAgent
    {
        public DocumentationAgent(AgentId id, AgentName name, PlatformType platform, IEnumerable<string>? focusAreas = null, IDictionary<string, object>? configuration = null)
            : base(id, name, platform, GetDefaultCapabilities(), focusAreas, configuration) { }

        private static IEnumerable<AgentCapability> GetDefaultCapabilities()
        {
            return new[] { AgentCapability.NaturalLanguageProcessing, AgentCapability.Documentation };
        }

        protected override Task OnInitializeAsync(AgentContext context) => Task.CompletedTask;
        protected override Task<AgentResult> OnExecuteAsync(AgentTask task) => Task.FromResult(AgentResult.Success("Documentation task completed"));
        protected override Task<AgentResult> OnCommunicateAsync(IAgent targetAgent, AgentMessage message) => Task.FromResult(AgentResult.Success("Communication handled"));
        protected override Task<AgentResult> OnLearnAsync(AgentExperience experience) => Task.FromResult(AgentResult.Success("Learning completed"));
        protected override Task OnShutdownAsync() => Task.CompletedTask;
        protected override Task OnRestoreAsync(AgentState state) => Task.CompletedTask;
    }

    public class CustomAgent : CrossPlatformAgent
    {
        public CustomAgent(AgentId id, AgentName name, PlatformType platform, IEnumerable<string>? focusAreas = null, IDictionary<string, object>? configuration = null)
            : base(id, name, platform, GetDefaultCapabilities(), focusAreas, configuration) { }

        private static IEnumerable<AgentCapability> GetDefaultCapabilities()
        {
            return new[] { AgentCapability.CodeGeneration, AgentCapability.CodeAnalysis };
        }

        protected override Task OnInitializeAsync(AgentContext context) => Task.CompletedTask;
        protected override Task<AgentResult> OnExecuteAsync(AgentTask task) => Task.FromResult(AgentResult.Success("Custom task completed"));
        protected override Task<AgentResult> OnCommunicateAsync(IAgent targetAgent, AgentMessage message) => Task.FromResult(AgentResult.Success("Communication handled"));
        protected override Task<AgentResult> OnLearnAsync(AgentExperience experience) => Task.FromResult(AgentResult.Success("Learning completed"));
        protected override Task OnShutdownAsync() => Task.CompletedTask;
        protected override Task OnRestoreAsync(AgentState state) => Task.CompletedTask;
    }
}
