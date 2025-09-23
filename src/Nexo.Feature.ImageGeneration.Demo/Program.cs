using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.Feature.ImageGeneration.Extensions;
using Nexo.Feature.ImageGeneration.Interfaces;
using Nexo.Feature.ImageGeneration.Models;
using Nexo.Feature.ImageGeneration.Services;
using Nexo.Feature.ImageGeneration.Tools;
using Spectre.Console;

namespace Nexo.Feature.ImageGeneration.Demo;

/// <summary>
/// Demo application for image generation services
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        // Check for demo mode
        var isDemoMode = args.Length > 0 && args[0] == "--demo";

        // Create host
        var host = CreateHost();

        // Get services
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var orchestrator = host.Services.GetRequiredService<ImageGenerationOrchestrator>();
        var imageGenerationTool = host.Services.GetRequiredService<ImageGenerationTool>();

        try
        {
            if (isDemoMode)
            {
                await RunDemoModeAsync(orchestrator, imageGenerationTool, logger);
            }
            else
            {
                await RunInteractiveModeAsync(orchestrator, imageGenerationTool, logger);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running image generation demo");
            AnsiConsole.WriteException(ex);
        }
    }

    private static IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Add image generation services with offline configuration
                services.AddOfflineImageGeneration();
                
                // Add logging
                services.AddLogging(builder => builder.AddConsole());
            })
            .Build();
    }

    private static async Task RunDemoModeAsync(
        ImageGenerationOrchestrator orchestrator,
        ImageGenerationTool imageGenerationTool,
        ILogger logger)
    {
        AnsiConsole.MarkupLine("[bold cyan]🎨 Nexo Image Generation Demo (Non-Interactive Mode)[/]");
        AnsiConsole.WriteLine();

        // Show available providers
        await ShowAvailableProvidersAsync(orchestrator);

        // Demo 1: Generate a simple image
        AnsiConsole.MarkupLine("[bold yellow]Demo 1: Generating a simple image[/]");
        var request1 = new ImageGenerationRequest
        {
            Prompt = "A beautiful sunset over a mountain landscape, digital art style",
            Width = 512,
            Height = 512,
            Count = 1
        };

        var result1 = await orchestrator.GenerateImagesAsync(request1);
        await DisplayResultAsync(result1, "Simple Image Generation");

        // Demo 2: Generate multiple images with different styles
        AnsiConsole.MarkupLine("[bold yellow]Demo 2: Generating multiple images with different styles[/]");
        var request2 = new ImageGenerationRequest
        {
            Prompt = "A futuristic city skyline at night",
            Width = 768,
            Height = 512,
            Count = 2,
            Style = "cyberpunk"
        };

        var result2 = await orchestrator.GenerateImagesAsync(request2);
        await DisplayResultAsync(result2, "Multiple Images with Style");

        // Demo 3: Use the Agent Tool
        AnsiConsole.MarkupLine("[bold yellow]Demo 3: Using the Agent Tool[/]");
        var toolRequest = new ImageGenerationToolRequest
        {
            Prompt = "A cute robot playing with a cat, cartoon style",
            Width = 512,
            Height = 512,
            Count = 1
        };

        var toolResult = await imageGenerationTool.ExecuteAsync(toolRequest, CreateToolContext());
        await DisplayToolResultAsync(toolResult, "Agent Tool Generation");

        // Show summary
        AnsiConsole.MarkupLine("[bold green]✅ Demo completed successfully![/]");
        AnsiConsole.MarkupLine("[dim]Generated images are saved to your Documents/Nexo/GeneratedImages folder[/]");
    }

    private static async Task RunInteractiveModeAsync(
        ImageGenerationOrchestrator orchestrator,
        ImageGenerationTool imageGenerationTool,
        ILogger logger)
    {
        AnsiConsole.MarkupLine("[bold cyan]🎨 Nexo Image Generation Interactive Demo[/]");
        AnsiConsole.WriteLine();

        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to do?")
                    .AddChoices(new[]
                    {
                        "Generate Images",
                        "Show Available Providers",
                        "Show Health Status",
                        "Use Agent Tool",
                        "Exit"
                    }));

            switch (choice)
            {
                case "Generate Images":
                    await GenerateImagesInteractiveAsync(orchestrator);
                    break;
                case "Show Available Providers":
                    await ShowAvailableProvidersAsync(orchestrator);
                    break;
                case "Show Health Status":
                    await ShowHealthStatusAsync(orchestrator);
                    break;
                case "Use Agent Tool":
                    await UseAgentToolInteractiveAsync(imageGenerationTool);
                    break;
                case "Exit":
                    AnsiConsole.MarkupLine("[bold green]Goodbye![/]");
                    return;
            }

            AnsiConsole.WriteLine();
        }
    }

    private static async Task GenerateImagesInteractiveAsync(ImageGenerationOrchestrator orchestrator)
    {
        AnsiConsole.MarkupLine("[bold yellow]🎨 Image Generation[/]");
        
        var prompt = AnsiConsole.Ask<string>("Enter your image prompt:");
        var width = AnsiConsole.Ask("Width (default 512):", 512);
        var height = AnsiConsole.Ask("Height (default 512):", 512);
        var count = AnsiConsole.Ask("Number of images (default 1):", 1);
        var style = AnsiConsole.Ask<string>("Style (optional):", "");

        var request = new ImageGenerationRequest
        {
            Prompt = prompt,
            Width = width,
            Height = height,
            Count = count,
            Style = string.IsNullOrEmpty(style) ? null : style
        };

        AnsiConsole.MarkupLine("[dim]Generating images...[/]");
        var result = await orchestrator.GenerateImagesAsync(request);
        await DisplayResultAsync(result, "Image Generation Result");
    }

    private static async Task UseAgentToolInteractiveAsync(ImageGenerationTool imageGenerationTool)
    {
        AnsiConsole.MarkupLine("[bold yellow]🤖 Agent Tool Usage[/]");
        
        var prompt = AnsiConsole.Ask<string>("Enter your image prompt:");
        var width = AnsiConsole.Ask("Width (default 512):", 512);
        var height = AnsiConsole.Ask("Height (default 512):", 512);

        var request = new ImageGenerationToolRequest
        {
            Prompt = prompt,
            Width = width,
            Height = height,
            Count = 1
        };

        AnsiConsole.MarkupLine("[dim]Executing agent tool...[/]");
        var result = await imageGenerationTool.ExecuteAsync(request, CreateToolContext());
        await DisplayToolResultAsync(result, "Agent Tool Result");
    }

    private static async Task ShowAvailableProvidersAsync(ImageGenerationOrchestrator orchestrator)
    {
        AnsiConsole.MarkupLine("[bold yellow]📋 Available Providers[/]");
        
        var providers = await orchestrator.GetAvailableProvidersAsync();
        
        var table = new Table();
        table.AddColumn("Provider");
        table.AddColumn("Status");
        table.AddColumn("Online Required");
        table.AddColumn("Models");

        foreach (var provider in providers)
        {
            var status = provider.IsHealthy ? "[green]Healthy[/]" : "[red]Unhealthy[/]";
            var onlineRequired = provider.RequiresOnline ? "[yellow]Yes[/]" : "[green]No[/]";
            
            table.AddRow(
                provider.DisplayName,
                status,
                onlineRequired,
                provider.AvailableModels.Count.ToString()
            );
        }

        AnsiConsole.Write(table);
    }

    private static async Task ShowHealthStatusAsync(ImageGenerationOrchestrator orchestrator)
    {
        AnsiConsole.MarkupLine("[bold yellow]🏥 Health Status[/]");
        
        var health = await orchestrator.GetHealthAsync();
        
        var status = health.IsHealthy ? "[green]Healthy[/]" : "[red]Unhealthy[/]";
        AnsiConsole.MarkupLine($"Overall Status: {status}");
        AnsiConsole.MarkupLine($"Healthy Providers: {health.HealthyProviders}/{health.TotalProviders}");
        
        foreach (var providerHealth in health.ProviderHealths)
        {
            var providerStatus = providerHealth.IsHealthy ? "[green]✓[/]" : "[red]✗[/]";
            AnsiConsole.MarkupLine($"{providerStatus} {providerHealth.Status} ({providerHealth.ResponseTimeMs}ms)");
        }
    }

    private static async Task DisplayResultAsync(ImageGenerationResult result, string title)
    {
        AnsiConsole.MarkupLine($"[bold cyan]{title}[/]");
        
        if (result.Success)
        {
            AnsiConsole.MarkupLine($"[green]✅ Success![/] Generated {result.Images.Count} image(s) in {result.GenerationTimeMs}ms");
            AnsiConsole.MarkupLine($"[dim]Model: {result.Metadata.Model}[/]");
            AnsiConsole.MarkupLine($"[dim]Provider: {result.Metadata.Provider}[/]");
            
            if (result.ImagePaths.Any())
            {
                AnsiConsole.MarkupLine("[dim]Saved to:[/]");
                foreach (var path in result.ImagePaths)
                {
                    AnsiConsole.MarkupLine($"[dim]  {path}[/]");
                }
            }
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]❌ Failed: {result.Error}[/]");
        }
        
        await Task.CompletedTask;
    }

    private static async Task DisplayToolResultAsync(ImageGenerationToolResult result, string title)
    {
        AnsiConsole.MarkupLine($"[bold cyan]{title}[/]");
        
        if (result.Success)
        {
            AnsiConsole.MarkupLine($"[green]✅ Success![/] Generated {result.Images.Count} image(s) in {result.GenerationTimeMs}ms");
            AnsiConsole.MarkupLine($"[dim]Tool: {result.ToolId}[/]");
            AnsiConsole.MarkupLine($"[dim]Model: {result.Metadata.Model}[/]");
            AnsiConsole.MarkupLine($"[dim]Provider: {result.Metadata.Provider}[/]");
            
            if (result.ImagePaths.Any())
            {
                AnsiConsole.MarkupLine("[dim]Saved to:[/]");
                foreach (var path in result.ImagePaths)
                {
                    AnsiConsole.MarkupLine($"[dim]  {path}[/]");
                }
            }
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]❌ Failed: {result.Error}[/]");
        }
        
        await Task.CompletedTask;
    }

    private static Nexo.Agent.Contracts.ToolContext CreateToolContext()
    {
        return new Nexo.Agent.Contracts.ToolContext(
            Logger: Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance,
            CancellationToken: System.Threading.CancellationToken.None,
            Timeout: TimeSpan.FromMinutes(5),
            Permissions: Nexo.Agent.Models.ToolPermissions.FileWrite,
            FileSystem: new Nexo.Agent.Implementations.DefaultFileSystem(),
            WorkingDirectory: Environment.CurrentDirectory
        );
    }
}
