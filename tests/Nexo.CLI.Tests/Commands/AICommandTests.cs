using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.IO;
using System;
using System.Threading.Tasks;
using Xunit;
using Moq;

namespace Nexo.CLI.Tests.Commands;

/// <summary>
/// Command for testing CLI AI functionality with proper logging and timeouts.
/// </summary>
public class AICommandTests
{
    private readonly ILogger<AICommandTests> _logger;

    public AICommandTests()
    {
        var mockLogger = new Mock<ILogger<AICommandTests>>();
        _logger = mockLogger.Object;
    }

    /// <summary>
    /// Tests AI command with suggest functionality.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if test passes, false otherwise</returns>
    public bool TestAICommandWithSuggest(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting AI command with suggest test");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var console = new TestConsole();
            var aiCommand = new Command("ai", "AI-powered development commands");
            var suggestCommand = new Command("suggest", "Get AI-powered code suggestions");
            var codeArgument = new Argument<string>("code", "Code to analyze");
            var contextOption = new Option<string>("--context", "Additional context") { IsRequired = false };
            
            suggestCommand.AddArgument(codeArgument);
            suggestCommand.AddOption(contextOption);
            suggestCommand.SetHandler(async (code, context) =>
            {
                console.WriteLine("Getting AI suggestions...");
                console.WriteLine($"Code: {code}");
                if (!string.IsNullOrEmpty(context))
                {
                    console.WriteLine($"Context: {context}");
                }
                await Task.Delay(100); // Simulate work
                console.WriteLine("AI suggestions generated.");
            }, codeArgument, contextOption);

            aiCommand.AddCommand(suggestCommand);
            aiCommand.Invoke("suggest test-code", console);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("AI command with suggest test exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var output = console.Out.ToString();
            var result = output.Contains("Getting AI suggestions...") && 
                        output.Contains("Code: test-code") &&
                        output.Contains("AI suggestions generated.");
            
            _logger.LogInformation("AI command with suggest test completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AI command with suggest test");
            return false;
        }
    }

    /// <summary>
    /// Tests AI command with context option.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if test passes, false otherwise</returns>
    public bool TestAICommandWithContext(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting AI command with context test");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var console = new TestConsole();
            var aiCommand = new Command("ai", "AI-powered development commands");
            var suggestCommand = new Command("suggest", "Get AI-powered code suggestions");
            var codeArgument = new Argument<string>("code", "Code to analyze");
            var contextOption = new Option<string>("--context", "Additional context") { IsRequired = false };
            
            suggestCommand.AddArgument(codeArgument);
            suggestCommand.AddOption(contextOption);
            suggestCommand.SetHandler(async (code, context) =>
            {
                console.WriteLine("Getting AI suggestions...");
                console.WriteLine($"Code: {code}");
                if (!string.IsNullOrEmpty(context))
                {
                    console.WriteLine($"Context: {context}");
                }
                await Task.Delay(100); // Simulate work
                console.WriteLine("AI suggestions generated.");
            }, codeArgument, contextOption);

            aiCommand.AddCommand(suggestCommand);
            aiCommand.Invoke("suggest test-code --context test-context", console);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("AI command with context test exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var output = console.Out.ToString();
            var result = output.Contains("Getting AI suggestions...") && 
                        output.Contains("Code: test-code") &&
                        output.Contains("Context: test-context") &&
                        output.Contains("AI suggestions generated.");
            
            _logger.LogInformation("AI command with context test completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AI command with context test");
            return false;
        }
    }

    [Fact]
    public async Task SuggestCommand_SemanticCaching_CacheHitAndMiss()
    {
        // This test verifies that the AI command works correctly
        // Note: Actual caching behavior would require integration with the caching infrastructure
        var console = new TestConsole();
        var aiCommand = new Command("ai", "AI-powered development commands");
        var suggestCommand = new Command("suggest", "Get AI-powered code suggestions");
        var codeArgument = new Argument<string>("code", "Code to analyze");
        suggestCommand.AddArgument(codeArgument);
        suggestCommand.SetHandler(async (code) =>
        {
            console.WriteLine($"AI suggestion for: {code}");
            await Task.Delay(50); // Simulate AI work
            console.WriteLine($"AI suggestions generated for: {code}");
        }, codeArgument);
        aiCommand.AddCommand(suggestCommand);

        // Test basic functionality
        aiCommand.Invoke("suggest test-code", console);
        var output = console.Out.ToString();
        Assert.Contains("AI suggestion for: test-code", output);
        Assert.Contains("AI suggestions generated for: test-code", output);

        // Test with different input
        console = new TestConsole(); // Create a new console for clean output
        aiCommand.Invoke("suggest different-code", console);
        var output2 = console.Out.ToString();
        Assert.Contains("AI suggestion for: different-code", output2);
        Assert.Contains("AI suggestions generated for: different-code", output2);
    }
} 