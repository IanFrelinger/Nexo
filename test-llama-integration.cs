using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Infrastructure.Services.AI;
using Nexo.Infrastructure.Services.Caching;

// Simple test to verify LLama integration works
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("AI Testing LLama Integration");
        Console.WriteLine("=============================");

        // Create service collection
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add HTTP client
        services.AddHttpClient();
        
        // Add cache service (mock)
        services.AddSingleton<ICacheService, MockCacheService>();
        
        // Add LLama providers
        services.AddSingleton<ILlamaProvider, OllamaProvider>();
        services.AddSingleton<ILlamaProvider, LlamaNativeProvider>();
        
        // Build service provider
        var serviceProvider = services.BuildServiceProvider();
        
        // Test Ollama provider
        Console.WriteLine("\n📡 Testing Ollama Provider...");
        var ollamaProvider = serviceProvider.GetRequiredService<OllamaProvider>();
        
        try
        {
            var healthStatus = await ollamaProvider.GetHealthStatusAsync();
            Console.WriteLine($"SUCCESS: Ollama Health: {healthStatus.Status}");
            Console.WriteLine($"   Response Time: {healthStatus.ResponseTimeMs}ms");
            Console.WriteLine($"   Is Healthy: {healthStatus.IsHealthy}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Ollama Error: {ex.Message}");
        }
        
        // Test Native provider
        Console.WriteLine("\nTool Testing Native Provider...");
        var nativeProvider = serviceProvider.GetRequiredService<LlamaNativeProvider>();
        
        try
        {
            var healthStatus = await nativeProvider.GetHealthStatusAsync();
            Console.WriteLine($"SUCCESS: Native Health: {healthStatus.Status}");
            Console.WriteLine($"   Response Time: {healthStatus.ResponseTimeMs}ms");
            Console.WriteLine($"   Is Healthy: {healthStatus.IsHealthy}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Native Error: {ex.Message}");
        }
        
        // Test model listing
        Console.WriteLine("\nList Testing Model Listing...");
        try
        {
            var ollamaModels = await ollamaProvider.GetAvailableModelsAsync();
            Console.WriteLine($"SUCCESS: Ollama Models: {ollamaModels.Count()}");
            
            var nativeModels = await nativeProvider.GetAvailableModelsAsync();
            Console.WriteLine($"SUCCESS: Native Models: {nativeModels.Count()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Model Listing Error: {ex.Message}");
        }
        
        Console.WriteLine("\nSUCCESS LLama Integration Test Complete!");
    }
}

// Mock cache service for testing
public class MockCacheService : ICacheService
{
    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<string?>(null);
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync(string key, string value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
