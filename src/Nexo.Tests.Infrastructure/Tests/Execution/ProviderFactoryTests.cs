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
            await TestExecuteLLMAsync_SelfExtendUnityBootstrap();
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

        // Mock providers require NEXO_ALLOW_MOCK=1 (disabled by default for production)
        WithEnv("NEXO_ALLOW_MOCK", "1", () =>
        {
            AssertTrue(factory.IsProviderAvailable("mock"));
            AssertTrue(factory.IsProviderAvailable("offline"));
            AssertTrue(factory.IsProviderAvailable("mock-json"));
            AssertTrue(factory.IsProviderAvailable("echo"));
        });
        WithEnv("NEXO_ALLOW_MOCK", null, () =>
        {
            AssertFalse(factory.IsProviderAvailable("mock"));
        });

        AssertTrue(factory.IsProviderAvailable("ollama"));
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

        // Mock provider requires NEXO_ALLOW_MOCK=1
        await WithEnv("NEXO_ALLOW_MOCK", "1", async () =>
        {
            var result = await factory.ExecuteLLMAsync(
                "mock",
                "You are a test",
                "Test prompt",
                new { },
                CancellationToken.None);

            AssertNotNull(result);
            AssertTrue(IsJsonObject(result), "mock provider should return JSON");
        });
        
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

    private async Task TestExecuteLLMAsync_SelfExtendUnityBootstrap()
    {
        var mockLogger = new Mock<ILogger<ProviderFactory>>();
        var factory = new ProviderFactory(mockLogger.Object);

        const string systemPrompt = """
You are a self-extending code agent. You may call tools to read/write files in the repository.
Current world state (JSON): {"RepoRoot":"/workspace","OutputRoot":"/workspace/out"}
Available tools:
- repo.fs.write: Write a file under the repo root
""";

        const string userPrompt = """
Objective:
Generate Unity bootstrap files (IGeneratedGameplaySystem, SystemContext, DashAbilitySystem).
""";

        var result = await WithEnv("NEXO_ALLOW_MOCK", "1", async () =>
            await factory.ExecuteLLMAsync("mock-json", systemPrompt, userPrompt, new { }, CancellationToken.None));

        using var doc = JsonDocument.Parse(result);
        AssertTrue(doc.RootElement.TryGetProperty("tool_calls", out var calls), "tool_calls should exist");
        AssertEqual(JsonValueKind.Array, calls.ValueKind);
        AssertTrue(calls.GetArrayLength() >= 3, "Expected at least 3 repo.fs.write calls");

        var first = calls[0];
        AssertEqual("repo.fs.write", first.GetProperty("id").GetString());
        var firstArgs = first.GetProperty("arguments");
        AssertEqual("/workspace", firstArgs.GetProperty("root").GetString());
        AssertEqual("docs/UnityBootstrapGenerated/IGeneratedGameplaySystem.cs", firstArgs.GetProperty("path").GetString());
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

    private static async Task<T> WithEnv<T>(string key, string? value, Func<Task<T>> action)
    {
        var old = Environment.GetEnvironmentVariable(key);
        try
        {
            Environment.SetEnvironmentVariable(key, value);
            return await action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, old);
        }
    }
}

