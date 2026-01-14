using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Infrastructure.Execution;
using System.Text.Json;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

/// <summary>
/// Tests for ProviderFactory.
/// </summary>
public class ProviderFactoryTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestIsProviderAvailable();
            await TestExecuteLLMAsync();
            
            return new TestResult
            {
                TestName = nameof(ProviderFactoryTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "All ProviderFactory tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                TestName = nameof(ProviderFactoryTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(ProviderFactoryTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }
    
    private async Task TestIsProviderAvailable()
    {
        var mockLogger = new Mock<ILogger<ProviderFactory>>();
        var factory = new ProviderFactory(mockLogger.Object);
        
        AssertTrue(factory.IsProviderAvailable("ollama"));
        AssertTrue(factory.IsProviderAvailable("mock"));
        AssertTrue(factory.IsProviderAvailable("offline"));
        AssertTrue(factory.IsProviderAvailable("mock-json"));
        AssertTrue(factory.IsProviderAvailable("echo"));
        AssertFalse(factory.IsProviderAvailable("unknown"));

        // Real providers depend on environment configuration; validate behavior is env-sensitive.
        WithEnv("OPENAI_API_KEY", null, () =>
        {
            AssertFalse(factory.IsProviderAvailable("openai"));
        });

        WithEnv("AZURE_OPENAI_ENDPOINT", null, () =>
        {
            WithEnv("AZURE_OPENAI_API_KEY", null, () =>
            {
                WithEnv("AZURE_OPENAI_DEPLOYMENT", null, () =>
                {
                    AssertFalse(factory.IsProviderAvailable("azure"));
                });
            });
        });
        
        await Task.CompletedTask;
    }
    
    private async Task TestExecuteLLMAsync()
    {
        var mockLogger = new Mock<ILogger<ProviderFactory>>();
        var factory = new ProviderFactory(mockLogger.Object);
        
        var result = await factory.ExecuteLLMAsync(
            "mock",
            "You are a test",
            "Test prompt",
            new { },
            CancellationToken.None);
        
        AssertNotNull(result);
        AssertTrue(IsJsonObject(result), "mock provider should return JSON");
        
        // openai should never throw just because it's not configured; it should fall back to mock-json.
        WithEnv("OPENAI_API_KEY", null, async () =>
        {
            var openAiResult = await factory.ExecuteLLMAsync(
                "openai",
                "Test",
                "Test",
                new { },
                CancellationToken.None);
            AssertNotNull(openAiResult);
            AssertTrue(IsJsonObject(openAiResult), "openai should fall back to JSON when not configured");
        }).GetAwaiter().GetResult();
        
        await Task.CompletedTask;
    }

    private static bool IsJsonObject(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var trimmed = text.Trim();
        if (!trimmed.StartsWith('{') || !trimmed.EndsWith('}')) return false;
        try
        {
            using var _ = JsonDocument.Parse(trimmed);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void WithEnv(string key, string? value, Action action)
    {
        var old = Environment.GetEnvironmentVariable(key);
        try
        {
            Environment.SetEnvironmentVariable(key, value);
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, old);
        }
    }

    private static async Task WithEnv(string key, string? value, Func<Task> action)
    {
        var old = Environment.GetEnvironmentVariable(key);
        try
        {
            Environment.SetEnvironmentVariable(key, value);
            await action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, old);
        }
    }
}

