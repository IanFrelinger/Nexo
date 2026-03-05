using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Infrastructure.Execution;
using System.Text.Json;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

/// <summary>
/// Tests for ProviderFactory.
/// </summary>
public class ProviderFactoryTests : UnitTestBase
{
    [Fact]
    public async Task ProviderFactory_AllTests_Pass()
    {
        var result = await ExecuteAsync(CancellationToken.None);
        Assert.True(result.Passed, result.ErrorMessage ?? result.Message);
    }

    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestIsProviderAvailable();
            await TestExecuteLLMAsync();
            await TestExecuteVisionAsync();
            
            return new TestResult
            {
                Name = nameof(ProviderFactoryTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "All ProviderFactory tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(ProviderFactoryTests),
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
                Name = nameof(ProviderFactoryTests),
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
        
        // openai when not configured should throw (fail fast, no mock fallback).
        WithEnv("OPENAI_API_KEY", null, async () =>
        {
            await AssertThrowsAsync<InvalidOperationException>(async () =>
                await factory.ExecuteLLMAsync("openai", "Test", "Test", new { }, CancellationToken.None));
        }).GetAwaiter().GetResult();

        // unknown provider should throw.
        await AssertThrowsAsync<InvalidOperationException>(async () =>
            await factory.ExecuteLLMAsync("unknown_provider", "Test", "Test", new { }, CancellationToken.None));

        await Task.CompletedTask;
    }

    private async Task TestExecuteVisionAsync()
    {
        var mockLogger = new Mock<ILogger<ProviderFactory>>();
        var factory = new ProviderFactory(mockLogger.Object);

        // Unknown vision provider should throw (fail fast).
        await AssertThrowsAsync<InvalidOperationException>(async () =>
            await factory.ExecuteVisionAsync(
                "unknown_vision",
                "System",
                "Describe the image",
                new byte[] { 0x89, 0x50, 0x4E },
                new { },
                CancellationToken.None));

        // openai vision: when OPENAI_API_KEY not set, fail fast with InvalidOperationException (same as LLM).
        WithEnv("OPENAI_API_KEY", null, async () =>
        {
            await AssertThrowsAsync<InvalidOperationException>(async () =>
                await factory.ExecuteVisionAsync(
                    "openai",
                    "System",
                    "Describe",
                    Array.Empty<byte>(),
                    new { },
                    CancellationToken.None));
        }).GetAwaiter().GetResult();
    }

    private static async Task AssertThrowsAsync<T>(Func<Task> action) where T : Exception
    {
        try
        {
            await action();
        }
        catch (T)
        {
            return;
        }
        throw new AssertionException($"Expected {typeof(T).Name} to be thrown");
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

