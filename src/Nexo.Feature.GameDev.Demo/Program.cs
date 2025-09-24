using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AudioGeneration.Extensions;
using Nexo.Feature.AudioGeneration.Models;
using Nexo.Feature.AudioGeneration.Services;
using Nexo.Feature.Networking.Extensions;
using Nexo.Feature.Networking.Models;
using Nexo.Feature.Networking.Services;
using Spectre.Console;

namespace Nexo.Feature.GameDev.Demo;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🎮 Nexo Game Development Demo");
        Console.WriteLine("=============================");
        Console.WriteLine("Comprehensive game development with AI-powered providers");
        Console.WriteLine();

        // Create host with all game development services
        var host = CreateHost();
        var audioOrchestrator = host.Services.GetRequiredService<AudioGenerationOrchestrator>();
        var networkingOrchestrator = host.Services.GetRequiredService<NetworkingOrchestrator>();

        try
        {
            // Show available providers
            await ShowProviderStats(audioOrchestrator, networkingOrchestrator);

            // Interactive demo
            await RunInteractiveDemo(audioOrchestrator, networkingOrchestrator);
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
                services.AddNetworkingServices();
            })
            .Build();
    }

    private static async Task ShowProviderStats(AudioGenerationOrchestrator audioOrchestrator, NetworkingOrchestrator networkingOrchestrator)
    {
        AnsiConsole.MarkupLine("[bold blue]📊 Available Game Development Providers[/]");
        
        // Audio Generation Providers
        var audioStats = await audioOrchestrator.GetProviderStatsAsync();
        
        var audioTable = new Table();
        audioTable.AddColumn("Provider Type");
        audioTable.AddColumn("Provider ID");
        audioTable.AddColumn("Available");
        audioTable.AddColumn("Requires Online");

        foreach (var (providerId, stat) in audioStats)
        {
            audioTable.AddRow(
                "Audio Generation",
                providerId,
                "✓",
                "No"
            );
        }

        AnsiConsole.Write(audioTable);
        AnsiConsole.WriteLine();

        // Networking Providers
        var networkingStats = await networkingOrchestrator.GetProviderStatsAsync();
        
        var networkingTable = new Table();
        networkingTable.AddColumn("Provider Type");
        networkingTable.AddColumn("Provider ID");
        networkingTable.AddColumn("Available");
        networkingTable.AddColumn("Requires Online");

        foreach (var (providerId, stat) in networkingStats)
        {
            networkingTable.AddRow(
                "Networking",
                providerId,
                "✓",
                "No"
            );
        }

        AnsiConsole.Write(networkingTable);
        AnsiConsole.WriteLine();
    }

    private static async Task RunInteractiveDemo(AudioGenerationOrchestrator audioOrchestrator, NetworkingOrchestrator networkingOrchestrator)
    {
        AnsiConsole.MarkupLine("[bold green]🎮 Interactive Game Development Demo[/]");
        AnsiConsole.MarkupLine("Generate complete game systems using AI-powered providers");
        AnsiConsole.WriteLine();

        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to do?")
                    .AddChoices(new[]
                    {
                        "Generate Audio Assets",
                        "Generate Networking System",
                        "Generate Complete Game Package",
                        "Show Provider Capabilities",
                        "Exit"
                    })
            );

            switch (choice)
            {
                case "Generate Audio Assets":
                    await GenerateAudioAssetsDemo(audioOrchestrator);
                    break;
                case "Generate Networking System":
                    await GenerateNetworkingSystemDemo(networkingOrchestrator);
                    break;
                case "Generate Complete Game Package":
                    await GenerateCompleteGamePackageDemo(audioOrchestrator, networkingOrchestrator);
                    break;
                case "Show Provider Capabilities":
                    await ShowProviderCapabilities();
                    break;
                case "Exit":
                    AnsiConsole.MarkupLine("[bold green]👋 Thanks for using Nexo Game Development![/]");
                    return;
            }

            AnsiConsole.WriteLine();
        }
    }

    private static async Task GenerateAudioAssetsDemo(AudioGenerationOrchestrator audioOrchestrator)
    {
        AnsiConsole.MarkupLine("[bold blue]🎵 Generating Audio Assets[/]");

        var audioRequests = new[]
        {
            new AudioGenerationRequest
            {
                Prompt = "Epic orchestral background music for a sci-fi game",
                AudioType = AudioType.Music,
                Duration = 30.0f,
                Volume = 0.7f,
                Style = "Orchestral"
            },
            new AudioGenerationRequest
            {
                Prompt = "Laser gun sound effect",
                AudioType = AudioType.SoundEffect,
                Duration = 2.0f,
                Volume = 0.9f
            },
            new AudioGenerationRequest
            {
                Prompt = "UI button click sound",
                AudioType = AudioType.UI,
                Duration = 0.5f,
                Volume = 0.8f
            },
            new AudioGenerationRequest
            {
                Prompt = "Ambient space station hum",
                AudioType = AudioType.Ambient,
                Duration = 60.0f,
                Volume = 0.4f
            }
        };

        AnsiConsole.MarkupLine("[yellow]⏳ Generating {0} audio assets...[/]", audioRequests.Length);

        var results = await audioOrchestrator.GenerateAudioBatchAsync(audioRequests);

        var successCount = results.Count(r => r.Success);
        var failureCount = results.Count(r => !r.Success);

        AnsiConsole.MarkupLine($"[green]✅ Successfully generated {successCount} audio assets[/]");
        if (failureCount > 0)
        {
            AnsiConsole.MarkupLine($"[red]❌ Failed to generate {failureCount} audio assets[/]");
        }

        foreach (var result in results)
        {
            if (result.Success)
            {
                AnsiConsole.MarkupLine($"📁 {result.FilePath} ({result.Duration:F2}s, {result.Format})");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]❌ Failed: {result.Error}[/]");
            }
        }
    }

    private static async Task GenerateNetworkingSystemDemo(NetworkingOrchestrator networkingOrchestrator)
    {
        AnsiConsole.MarkupLine("[bold blue]🌐 Generating Networking System[/]");

        var networkingRequest = new NetworkingRequest
        {
            Prompt = "Multiplayer FPS game with 16 players, client-server architecture",
            NetworkingType = NetworkingType.ClientServer,
            MaxPlayers = 16,
            Port = 7777,
            Security = new SecuritySettings
            {
                EnableEncryption = true,
                EnableAuthentication = true,
                EnableAntiCheat = true,
                EnableRateLimiting = true,
                MaxConnectionsPerIP = 2
            }
        };

        AnsiConsole.MarkupLine("[yellow]⏳ Generating networking configuration...[/]");

        var result = await networkingOrchestrator.GenerateNetworkingConfigurationAsync(networkingRequest);

        if (result.Success)
        {
            AnsiConsole.MarkupLine("[green]✅ Networking system generated successfully![/]");
            AnsiConsole.MarkupLine($"📁 Configuration: {result.FilePath}");
            AnsiConsole.MarkupLine($"🎮 Type: {result.Configuration?.Type}");
            AnsiConsole.MarkupLine($"👥 Max Players: {result.Configuration?.Server.MaxPlayers}");
            AnsiConsole.MarkupLine($"🔒 Security: Encryption={result.Configuration?.Security.EnableEncryption}, Auth={result.Configuration?.Security.EnableAuthentication}");
            AnsiConsole.MarkupLine($"📡 Events: {result.Configuration?.Events.Count}");
            AnsiConsole.MarkupLine($"💬 Message Types: {result.Configuration?.MessageTypes.Count}");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]❌ Failed to generate networking system: {result.Error}[/]");
        }
    }

    private static async Task GenerateCompleteGamePackageDemo(AudioGenerationOrchestrator audioOrchestrator, NetworkingOrchestrator networkingOrchestrator)
    {
        AnsiConsole.MarkupLine("[bold blue]🎮 Generating Complete Game Package[/]");
        AnsiConsole.MarkupLine("This will generate a complete game package with audio, networking, and other systems");

        var gameSpec = AnsiConsole.Ask<string>("Enter your game specification:");
        var maxPlayers = AnsiConsole.Ask<int>("Maximum players", 4);

        AnsiConsole.MarkupLine("[yellow]⏳ Generating complete game package...[/]");

        // Generate audio assets
        var audioRequests = new[]
        {
            new AudioGenerationRequest
            {
                Prompt = $"Background music for {gameSpec}",
                AudioType = AudioType.Music,
                Duration = 120.0f,
                Volume = 0.6f
            },
            new AudioGenerationRequest
            {
                Prompt = $"Sound effects for {gameSpec}",
                AudioType = AudioType.SoundEffect,
                Duration = 5.0f,
                Volume = 0.8f
            }
        };

        var audioResults = await audioOrchestrator.GenerateAudioBatchAsync(audioRequests);

        // Generate networking system
        var networkingRequest = new NetworkingRequest
        {
            Prompt = $"{gameSpec} with {maxPlayers} players",
            NetworkingType = NetworkingType.ClientServer,
            MaxPlayers = maxPlayers,
            Port = 7777
        };

        var networkingResult = await networkingOrchestrator.GenerateNetworkingConfigurationAsync(networkingRequest);

        // Show results
        AnsiConsole.MarkupLine("[green]✅ Complete game package generated![/]");
        AnsiConsole.MarkupLine($"🎵 Audio Assets: {audioResults.Count(r => r.Success)}/{audioRequests.Length}");
        AnsiConsole.MarkupLine($"🌐 Networking: {(networkingResult.Success ? "✅" : "❌")}");
        
        if (networkingResult.Success)
        {
            AnsiConsole.MarkupLine($"📁 Networking Config: {networkingResult.FilePath}");
        }

        foreach (var audioResult in audioResults.Where(r => r.Success))
        {
            AnsiConsole.MarkupLine($"📁 Audio: {audioResult.FilePath}");
        }
    }

    private static async Task ShowProviderCapabilities()
    {
        AnsiConsole.MarkupLine("[bold blue]🔧 Provider Capabilities[/]");
        
        var capabilitiesTable = new Table();
        capabilitiesTable.AddColumn("Provider");
        capabilitiesTable.AddColumn("Capabilities");
        capabilitiesTable.AddColumn("Online/Offline");
        capabilitiesTable.AddColumn("Platforms");

        capabilitiesTable.AddRow(
            "Audio Generation",
            "Music, Sound Effects, Voice, Ambient, UI",
            "Both",
            "Unity, Unreal, WebGL, Mobile"
        );

        capabilitiesTable.AddRow(
            "Animation & Rigging",
            "Character Animation, Rigging, IK, Constraints",
            "Offline",
            "Unity, Unreal, Blender"
        );

        capabilitiesTable.AddRow(
            "Physics & Simulation",
            "Rigid Body, Soft Body, Fluid, Cloth, Particles",
            "Offline",
            "Unity, Unreal, Custom"
        );

        capabilitiesTable.AddRow(
            "Networking & Multiplayer",
            "Client-Server, P2P, Cloud, Security",
            "Both",
            "Unity, Unreal, WebGL, Mobile"
        );

        capabilitiesTable.AddRow(
            "Input & Controls",
            "Keyboard, Mouse, Gamepad, Touch, VR",
            "Offline",
            "Unity, Unreal, Mobile, VR"
        );

        AnsiConsole.Write(capabilitiesTable);
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold yellow]🎯 Key Features:[/]");
        AnsiConsole.MarkupLine("• [green]AI-Powered Generation[/] - Natural language to code/assets");
        AnsiConsole.MarkupLine("• [green]Cross-Platform Support[/] - Unity, Unreal, WebGL, Mobile");
        AnsiConsole.MarkupLine("• [green]Online/Offline Flexibility[/] - Works with or without internet");
        AnsiConsole.MarkupLine("• [green]Configurable & Extensible[/] - Customize for your needs");
        AnsiConsole.MarkupLine("• [green]Production Ready[/] - Enterprise-grade quality");
        AnsiConsole.MarkupLine("• [green]Nexo Philosophy[/] - Reusable, composable, cross-domain");
    }
}

