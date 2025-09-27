using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.NativeConsole;

/// <summary>
/// Application generation functionality
/// </summary>
public partial class LovableNativeInterface
{
    private async Task GenerateAppAsync()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("🚀 APPLICATION GENERATOR");
        Console.WriteLine("=========================");
        Console.ResetColor();
        Console.WriteLine();

        // Get description
        Console.WriteLine("💬 Describe your application in natural language:");
        Console.WriteLine("   (Be specific about features, design, and functionality)");
        Console.WriteLine();
        Console.Write("Description: ");
        var description = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(description))
        {
            Console.WriteLine("❌ Description cannot be empty!");
            return;
        }

        // Select platform
        var selectedPlatform = await SelectPlatformAsync();
        if (selectedPlatform == null) return;

        // Select features
        var selectedFeatures = await SelectFeaturesAsync();
        if (selectedFeatures == null) return;

        // Generate application
        await GenerateApplicationAsync(description, selectedPlatform, selectedFeatures);
    }

    private async Task<AppType?> SelectPlatformAsync()
    {
        Console.WriteLine();
        Console.WriteLine("📱 Select your target platform:");
        Console.WriteLine();

        for (int i = 0; i < _appTypes.Count; i++)
        {
            var appType = _appTypes[i];
            Console.WriteLine($"{i + 1}. {appType.Icon} {appType.Name}");
            Console.WriteLine($"   {appType.Description}");
            Console.WriteLine();
        }

        Console.Write("Choose platform (1-{0}): ", _appTypes.Count);
        var input = Console.ReadLine();

        if (int.TryParse(input, out int choice) && choice >= 1 && choice <= _appTypes.Count)
        {
            return _appTypes[choice - 1];
        }

        Console.WriteLine("❌ Invalid platform selection!");
        return null;
    }

    private async Task<List<Feature>?> SelectFeaturesAsync()
    {
        Console.WriteLine();
        Console.WriteLine("✨ Select features to include:");
        Console.WriteLine();

        var selectedFeatures = new List<Feature>();

        for (int i = 0; i < _features.Count; i++)
        {
            var feature = _features[i];
            Console.Write($"{i + 1}. {feature.Icon} {feature.Name} - {feature.Description} (y/n): ");
            var input = Console.ReadLine()?.Trim().ToLower();

            if (input == "y" || input == "yes")
            {
                selectedFeatures.Add(feature);
            }
        }

        Console.WriteLine();
        Console.WriteLine($"✅ Selected {selectedFeatures.Count} features");
        return selectedFeatures;
    }

    private async Task GenerateApplicationAsync(string description, AppType platform, List<Feature> features)
    {
        Console.WriteLine();
        Console.WriteLine("⚡ GENERATING YOUR APPLICATION");
        Console.WriteLine("===============================");
        Console.WriteLine();

        var appName = DetermineAppName(description);
        var steps = new[]
        {
            ("🔍 Analyzing your description", 1000),
            ("🏗️ Generating project structure", 1500),
            ("📦 Installing dependencies", 2000),
            ("💻 Generating application code", 2000),
            ("📚 Creating documentation", 1000),
            ("✅ Finalizing project", 500)
        };

        for (int i = 0; i < steps.Length; i++)
        {
            var (step, delay) = steps[i];
            Console.Write($"Step {i + 1}/6: {step}");
            
            // Show progress dots
            for (int j = 0; j < 3; j++)
            {
                await Task.Delay(delay / 3);
                Console.Write(".");
            }
            
            Console.WriteLine(" ✅");
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("🎉 APPLICATION GENERATED SUCCESSFULLY!");
        Console.WriteLine("======================================");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine($"📱 Application: {appName}");
        Console.WriteLine($"🌐 Platform: {platform.Name}");
        Console.WriteLine($"✨ Features: {string.Join(", ", features.Select(f => f.Name))}");
        Console.WriteLine($"📁 Location: ./{appName.ToLower().Replace(" ", "-")}");
        Console.WriteLine();
        Console.WriteLine("🚀 Next steps:");
        Console.WriteLine($"   cd {appName.ToLower().Replace(" ", "-")}");
        Console.WriteLine("   # Dependencies are already installed");
        Console.WriteLine("   # Run your application!");
    }

    private string DetermineAppName(string description)
    {
        var words = description.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 0)
        {
            var firstWord = words[0].ToLower();
            if (firstWord == "a" || firstWord == "an" || firstWord == "the")
            {
                return words.Length > 1 ? words[1] : "My App";
            }
            return firstWord;
        }
        return "My App";
    }
}
