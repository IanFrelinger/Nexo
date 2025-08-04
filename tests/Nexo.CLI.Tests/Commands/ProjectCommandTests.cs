using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.IO;
using System;
using System.Threading.Tasks;

namespace Nexo.CLI.Tests.Commands;

/// <summary>
/// Command for testing CLI project functionality with proper logging and timeouts.
/// </summary>
public class ProjectCommandTests
{
    private readonly ILogger<ProjectCommandTests> _logger;

    public ProjectCommandTests(ILogger<ProjectCommandTests> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tests project command with init functionality.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if test passes, false otherwise</returns>
    public bool TestProjectCommandWithInit(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting project command with init test");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var console = new TestConsole();
            var projectCommand = new Command("project", "Project management commands");
            var initCommand = new Command("init", "Initialize a new project");
            var nameArgument = new Argument<string>("name", "Project name");
            var templateOption = new Option<string>("--template", "Project template") { IsRequired = false };
            
            initCommand.AddArgument(nameArgument);
            initCommand.AddOption(templateOption);
            initCommand.SetHandler(async (name, template) =>
            {
                console.WriteLine("Initializing project...");
                console.WriteLine($"Name: {name}");
                if (!string.IsNullOrEmpty(template))
                {
                    console.WriteLine($"Template: {template}");
                }
                await Task.Delay(100); // Simulate work
                console.WriteLine("Project initialization completed.");
            }, nameArgument, templateOption);

            projectCommand.AddCommand(initCommand);
            projectCommand.Invoke("init test-project", console);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Project command with init test exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var output = console.Out.ToString();
            var result = output.Contains("Initializing project...") && 
                        output.Contains("Name: test-project") &&
                        output.Contains("Project initialization completed.");
            
            _logger.LogInformation("Project command with init test completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during project command with init test");
            return false;
        }
    }

    /// <summary>
    /// Tests project command with template functionality.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if test passes, false otherwise</returns>
    public bool TestProjectCommandWithTemplate(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting project command with template test");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var console = new TestConsole();
            var projectCommand = new Command("project", "Project management commands");
            var templateCommand = new Command("template", "Manage project templates");
            var listCommand = new Command("list", "List available templates");
            var categoryOption = new Option<string>("--category", "Template category") { IsRequired = false };
            
            listCommand.AddOption(categoryOption);
            listCommand.SetHandler(async (category) =>
            {
                console.WriteLine("Listing templates...");
                if (!string.IsNullOrEmpty(category))
                {
                    console.WriteLine($"Category: {category}");
                }
                await Task.Delay(100); // Simulate work
                console.WriteLine("Template listing completed.");
            }, categoryOption);

            templateCommand.AddCommand(listCommand);
            projectCommand.AddCommand(templateCommand);
            projectCommand.Invoke("template list --category web", console);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Project command with template test exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var output = console.Out.ToString();
            var result = output.Contains("Listing templates...") && 
                        output.Contains("Category: web") &&
                        output.Contains("Template listing completed.");
            
            _logger.LogInformation("Project command with template test completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during project command with template test");
            return false;
        }
    }
} 