using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Xunit;
using Nexo.CLI.Interactive;
using Nexo.CLI.Dashboard;
using Nexo.CLI.Progress;
using Nexo.CLI.Help;

namespace Nexo.CLI.Tests
{
    /// <summary>
    /// Tests for enhanced CLI functionality
    /// </summary>
    public class EnhancedCLITests
    {
        [Fact]
        public void CanCreateInteractiveCLI()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddEnhancedCLIServices();
            
            var serviceProvider = services.BuildServiceProvider();
            
            // Act
            var interactiveCLI = serviceProvider.GetService<IInteractiveCLI>();
            
            // Assert
            Assert.NotNull(interactiveCLI);
            Assert.IsType<InteractiveCLI>(interactiveCLI);
        }
        
        [Fact]
        public void CanCreateRealTimeDashboard()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddEnhancedCLIServices();
            
            var serviceProvider = services.BuildServiceProvider();
            
            // Act
            var dashboard = serviceProvider.GetService<IRealTimeDashboard>();
            
            // Assert
            Assert.NotNull(dashboard);
            Assert.IsType<RealTimeDashboard>(dashboard);
        }
        
        [Fact]
        public void CanCreateProgressTracker()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddEnhancedCLIServices();
            
            var serviceProvider = services.BuildServiceProvider();
            
            // Act
            var progressTracker = serviceProvider.GetService<IProgressTracker>();
            
            // Assert
            Assert.NotNull(progressTracker);
            Assert.IsType<ProgressTracker>(progressTracker);
        }
        
        [Fact]
        public void CanCreateHelpSystem()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddEnhancedCLIServices();
            
            var serviceProvider = services.BuildServiceProvider();
            
            // Act
            var helpSystem = serviceProvider.GetService<IInteractiveHelpSystem>();
            
            // Assert
            Assert.NotNull(helpSystem);
            Assert.IsType<InteractiveHelpSystem>(helpSystem);
        }
        
        [Fact]
        public void CanCreateCLIStateManager()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddEnhancedCLIServices();
            
            var serviceProvider = services.BuildServiceProvider();
            
            // Act
            var stateManager = serviceProvider.GetService<ICLIStateManager>();
            
            // Assert
            Assert.NotNull(stateManager);
            Assert.IsType<CLIStateManager>(stateManager);
        }
        
        [Fact]
        public async Task CLIStateManagerCanManageContext()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddEnhancedCLIServices();
            
            var serviceProvider = services.BuildServiceProvider();
            var stateManager = serviceProvider.GetRequiredService<ICLIStateManager>();
            
            // Act
            var context = await stateManager.GetCurrentContextAsync();
            
            // Assert
            Assert.NotNull(context);
            Assert.NotNull(context.RecentCommands);
            Assert.NotNull(context.UserPreferences);
        }
        
        [Fact]
        public async Task CommandSuggestionEngineCanProvideSuggestions()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddEnhancedCLIServices();
            
            var serviceProvider = services.BuildServiceProvider();
            var suggestionEngine = serviceProvider.GetRequiredService<ICommandSuggestionEngine>();
            
            // Act
            var completions = await suggestionEngine.GetCompletionsAsync("proj");
            
            // Assert
            Assert.NotNull(completions);
        }
        
        [Fact]
        public async Task HelpSystemCanGenerateDocumentation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddEnhancedCLIServices();
            
            var serviceProvider = services.BuildServiceProvider();
            var helpSystem = serviceProvider.GetRequiredService<IInteractiveHelpSystem>();
            
            // Act & Assert - Should not throw
            await helpSystem.ShowInteractiveHelp();
        }
        
        [Fact]
        public async Task ExampleRepositoryCanProvideExamples()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddEnhancedCLIServices();
            
            var serviceProvider = services.BuildServiceProvider();
            var exampleRepository = serviceProvider.GetRequiredService<IExampleRepository>();
            
            // Act
            var examples = await exampleRepository.GetAllExamplesAsync();
            
            // Assert
            Assert.NotNull(examples);
            Assert.NotEmpty(examples);
        }
    }
}
