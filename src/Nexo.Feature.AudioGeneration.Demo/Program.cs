using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AudioGeneration.Extensions;
using Nexo.Feature.AudioGeneration.Interfaces;
using Nexo.Feature.AudioGeneration.Models;
using Nexo.Feature.AudioGeneration.Services;
using Spectre.Console;

namespace Nexo.Feature.AudioGeneration.Demo;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🎵 Nexo Audio Generation Demo");
        Console.WriteLine("=============================");

        // Create host with audio generation services
        var host = CreateHost();
        var orchestrator = host.Services.GetRequiredService<AudioGenerationOrchestrator>();

        try
        {
            // Show available providers
            await ShowProviderStats(orchestrator);

            // Interactive demo
            await RunInteractiveDemo(orchestrator);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }
    }

    private static IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddAudioGenerationServices();
            })
            .Build();
    }

    private static async Task ShowProviderStats(AudioGenerationOrchestrator orchestrator)
    {
        AnsiConsole.MarkupLine("[bold blue]📊 Available Audio Generation Providers[/]");
        
        var stats = await orchestrator.GetProviderStatsAsync();
        
        var table = new Table();
        table.AddColumn("Provider ID");
        table.AddColumn("Display Name");
        table.AddColumn("Available");
        table.AddColumn("Requires Online");
        table.AddColumn("Model Count");

        foreach (var (providerId, stat) in stats)
        {
            // Get provider info from the orchestrator
            var providers = orchestrator.GetType().GetField("_providers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (providers?.GetValue(orchestrator) is IEnumerable<IAudioGenerationProvider> providerList)
            {
                var provider = providerList.FirstOrDefault(p => p.ProviderId == providerId);
                if (provider != null)
                {
                    table.AddRow(
                        providerId,
                        provider.DisplayName,
                        provider.IsAvailable ? "[green]✓[/]" : "[red]✗[/]",
                        provider.RequiresOnline ? "[yellow]Yes[/]" : "[green]No[/]",
                        "3" // Mock model count
                    );
                }
            }
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static async Task RunInteractiveDemo(AudioGenerationOrchestrator orchestrator)
    {
        AnsiConsole.MarkupLine("[bold green]🎮 Interactive Audio Generation Demo[/]");
        AnsiConsole.MarkupLine("Generate audio from text prompts using AI models");
        AnsiConsole.WriteLine();

        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to do?")
                    .AddChoices(new[]
                    {
                        "Generate Sound Effect",
                        "Generate Music",
                        "Generate Voice",
                        "Generate Ambient Sound",
                        "Generate UI Sound",
                        "Generate Environmental Sound",
                        "Batch Generation Demo",
                        "Exit"
                    })
            );

            switch (choice)
            {
                case "Generate Sound Effect":
                    await GenerateAudioDemo(orchestrator, AudioType.SoundEffect);
                    break;
                case "Generate Music":
                    await GenerateAudioDemo(orchestrator, AudioType.Music);
                    break;
                case "Generate Voice":
                    await GenerateAudioDemo(orchestrator, AudioType.Voice);
                    break;
                case "Generate Ambient Sound":
                    await GenerateAudioDemo(orchestrator, AudioType.Ambient);
                    break;
                case "Generate UI Sound":
                    await GenerateAudioDemo(orchestrator, AudioType.UI);
                    break;
                case "Generate Environmental Sound":
                    await GenerateAudioDemo(orchestrator, AudioType.Environmental);
                    break;
                case "Batch Generation Demo":
                    await BatchGenerationDemo(orchestrator);
                    break;
                case "Exit":
                    AnsiConsole.MarkupLine("[bold green]👋 Thanks for using Nexo Audio Generation![/]");
                    return;
            }

            AnsiConsole.WriteLine();
        }
    }

    private static async Task GenerateAudioDemo(AudioGenerationOrchestrator orchestrator, AudioType audioType)
    {
        AnsiConsole.MarkupLine($"[bold blue]🎵 Generating {audioType} Audio[/]");

        var prompt = AnsiConsole.Ask<string>("Enter your audio prompt:");
        var duration = AnsiConsole.Ask<float>("Duration (seconds)", 5.0f);
        var volume = AnsiConsole.Ask<float>("Volume (0.0-1.0)", 0.8f);

        var request = new AudioGenerationRequest
        {
            Prompt = prompt,
            AudioType = audioType,
            Duration = duration,
            Volume = volume,
            SampleRate = 44100,
            Channels = 2
        };

        AnsiConsole.MarkupLine("[yellow]⏳ Generating audio...[/]");

        var result = await orchestrator.GenerateAudioAsync(request);

        if (result.Success)
        {
            AnsiConsole.MarkupLine("[green]✅ Audio generated successfully![/]");
            AnsiConsole.MarkupLine($"📁 File: {result.FilePath}");
            AnsiConsole.MarkupLine($"⏱️ Duration: {result.Duration:F2}s");
            AnsiConsole.MarkupLine($"🎛️ Sample Rate: {result.SampleRate} Hz");
            AnsiConsole.MarkupLine($"🔊 Channels: {result.Channels}");
            AnsiConsole.MarkupLine($"⏱️ Generation Time: {result.GenerationTimeMs}ms");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]❌ Failed to generate audio: {result.Error}[/]");
        }
    }

    private static async Task BatchGenerationDemo(AudioGenerationOrchestrator orchestrator)
    {
        AnsiConsole.MarkupLine("[bold blue]🎵 Batch Audio Generation Demo[/]");

        var requests = new[]
        {
            new AudioGenerationRequest
            {
                Prompt = "Gunshot sound effect",
                AudioType = AudioType.SoundEffect,
                Duration = 2.0f,
                Volume = 0.9f
            },
            new AudioGenerationRequest
            {
                Prompt = "Background music for a sci-fi game",
                AudioType = AudioType.Music,
                Duration = 10.0f,
                Volume = 0.6f
            },
            new AudioGenerationRequest
            {
                Prompt = "UI click sound",
                AudioType = AudioType.UI,
                Duration = 0.5f,
                Volume = 0.7f
            }
        };

        AnsiConsole.MarkupLine("[yellow]⏳ Generating {0} audio clips in batch...[/]", requests.Length);

        var results = await orchestrator.GenerateAudioBatchAsync(requests);

        var successCount = results.Count(r => r.Success);
        var failureCount = results.Count(r => !r.Success);

        AnsiConsole.MarkupLine($"[green]✅ Successfully generated {successCount} audio clips[/]");
        if (failureCount > 0)
        {
            AnsiConsole.MarkupLine($"[red]❌ Failed to generate {failureCount} audio clips[/]");
        }

        foreach (var result in results)
        {
            if (result.Success)
            {
                AnsiConsole.MarkupLine($"📁 {result.FilePath} ({result.Duration:F2}s)");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]❌ Failed: {result.Error}[/]");
            }
        }
    }
}
