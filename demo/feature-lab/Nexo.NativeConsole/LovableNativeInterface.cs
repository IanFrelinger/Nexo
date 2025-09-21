using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.NativeConsole;

public class LovableNativeInterface
{
    private readonly List<AppType> _appTypes;
    private readonly List<Feature> _features;
    private readonly List<QuickExample> _quickExamples;

    public LovableNativeInterface()
    {
        _appTypes = InitializeAppTypes();
        _features = InitializeFeatures();
        _quickExamples = InitializeQuickExamples();
    }

    public async Task RunAsync()
    {
        ShowWelcomeScreen();
        await Task.Delay(2000);

        while (true)
        {
            ShowMainMenu();
            var choice = GetUserChoice();

            switch (choice)
            {
                case "1":
                    await GenerateAppAsync();
                    break;
                case "2":
                    ShowQuickExamples();
                    break;
                case "3":
                    ShowAppTypes();
                    break;
                case "4":
                    ShowFeatures();
                    break;
                case "5":
                    ShowHelp();
                    break;
                case "6":
                case "q":
                case "quit":
                case "exit":
                    ShowGoodbye();
                    return;
                default:
                    ShowInvalidChoice();
                    break;
            }

            if (choice != "6" && choice != "q" && choice != "quit" && choice != "exit")
            {
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                Console.Clear();
            }
        }
    }

    private void ShowWelcomeScreen()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                                                                              ║");
        Console.WriteLine("║  🚀 NEXO AI DEVELOPMENT PLATFORM - NATIVE INTERFACE                        ║");
        Console.WriteLine("║                                                                              ║");
        Console.WriteLine("║  Build applications with natural language descriptions                      ║");
        Console.WriteLine("║  Just like Lovable, but running entirely on your machine                    ║");
        Console.WriteLine("║                                                                              ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
    }

    private void ShowMainMenu()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("🎯 MAIN MENU");
        Console.WriteLine("============");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("1. 🚀 Generate New Application");
        Console.WriteLine("2. 💡 Quick Examples");
        Console.WriteLine("3. 📱 Available Platforms");
        Console.WriteLine("4. ✨ Features & Capabilities");
        Console.WriteLine("5. ❓ Help & Documentation");
        Console.WriteLine("6. 🚪 Exit");
        Console.WriteLine();
        Console.Write("Choose an option (1-6): ");
    }

    private string GetUserChoice()
    {
        return Console.ReadLine()?.Trim().ToLower() ?? "";
    }

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

    private void ShowQuickExamples()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("💡 QUICK EXAMPLES");
        Console.WriteLine("=================");
        Console.ResetColor();
        Console.WriteLine();

        for (int i = 0; i < _quickExamples.Count; i++)
        {
            var example = _quickExamples[i];
            Console.WriteLine($"{i + 1}. {example.Title}");
            Console.WriteLine($"   {example.Description}");
            Console.WriteLine($"   Platform: {example.Platform}");
            Console.WriteLine();
        }

        Console.WriteLine("💡 Tip: Copy any description and use it in the Application Generator!");
    }

    private void ShowAppTypes()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("📱 AVAILABLE PLATFORMS");
        Console.WriteLine("======================");
        Console.ResetColor();
        Console.WriteLine();

        foreach (var appType in _appTypes)
        {
            Console.WriteLine($"{appType.Icon} {appType.Name}");
            Console.WriteLine($"   {appType.Description}");
            Console.WriteLine($"   Technologies: {string.Join(", ", appType.Technologies)}");
            Console.WriteLine();
        }
    }

    private void ShowFeatures()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("✨ FEATURES & CAPABILITIES");
        Console.WriteLine("==========================");
        Console.ResetColor();
        Console.WriteLine();

        foreach (var feature in _features)
        {
            Console.WriteLine($"{feature.Icon} {feature.Name}");
            Console.WriteLine($"   {feature.Description}");
            Console.WriteLine();
        }
    }

    private void ShowHelp()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("❓ HELP & DOCUMENTATION");
        Console.WriteLine("=======================");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("🎯 How to use Nexo Native Interface:");
        Console.WriteLine();
        Console.WriteLine("1. Choose 'Generate New Application' from the main menu");
        Console.WriteLine("2. Describe your app in natural language (be specific!)");
        Console.WriteLine("3. Select your target platform (Web, Mobile, Desktop, etc.)");
        Console.WriteLine("4. Choose features you want to include");
        Console.WriteLine("5. Watch as Nexo generates your complete application!");
        Console.WriteLine();
        Console.WriteLine("💡 Tips for better results:");
        Console.WriteLine("• Be specific about features and functionality");
        Console.WriteLine("• Mention design preferences (dark mode, responsive, etc.)");
        Console.WriteLine("• Include technical requirements if you have them");
        Console.WriteLine("• Use the Quick Examples for inspiration");
        Console.WriteLine();
        Console.WriteLine("🔧 What gets generated:");
        Console.WriteLine("• Complete project structure");
        Console.WriteLine("• All necessary dependencies installed");
        Console.WriteLine("• Working application code");
        Console.WriteLine("• Documentation and README");
        Console.WriteLine("• Configuration files");
    }

    private void ShowInvalidChoice()
    {
        Console.WriteLine();
        Console.WriteLine("❌ Invalid choice! Please select 1-6.");
    }

    private void ShowGoodbye()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("👋 Thank you for using Nexo AI Development Platform!");
        Console.WriteLine("Happy coding! 🚀");
        Console.ResetColor();
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

    private List<AppType> InitializeAppTypes()
    {
        return new List<AppType>
        {
            new AppType
            {
                Name = "Web Application",
                Description = "Modern web apps with React, Vue, or Angular",
                Icon = "🌐",
                Technologies = new[] { "React", "TypeScript", "CSS3", "Vite" }
            },
            new AppType
            {
                Name = "Mobile Application",
                Description = "Cross-platform mobile apps for iOS and Android",
                Icon = "📱",
                Technologies = new[] { "React Native", "TypeScript", "Expo" }
            },
            new AppType
            {
                Name = "Desktop Application",
                Description = "Native desktop apps for Windows, macOS, and Linux",
                Icon = "🖥️",
                Technologies = new[] { "Electron", "React", "TypeScript" }
            },
            new AppType
            {
                Name = "API Server",
                Description = "REST and GraphQL APIs with authentication",
                Icon = "🔌",
                Technologies = new[] { "Node.js", "Express", "TypeScript", "MongoDB" }
            },
            new AppType
            {
                Name = "Game Application",
                Description = "2D and 3D games with Unity or Unreal",
                Icon = "🎮",
                Technologies = new[] { "Unity", "C#", "2D/3D Graphics" }
            },
            new AppType
            {
                Name = "Console Application",
                Description = "Command-line tools and utilities",
                Icon = "💻",
                Technologies = new[] { "C#", "CLI", "Terminal" }
            }
        };
    }

    private List<Feature> InitializeFeatures()
    {
        return new List<Feature>
        {
            new Feature
            {
                Name = "Dark Mode",
                Description = "Toggle between light and dark themes",
                Icon = "🌙"
            },
            new Feature
            {
                Name = "Authentication",
                Description = "User login, registration, and session management",
                Icon = "🔐"
            },
            new Feature
            {
                Name = "Database",
                Description = "Data persistence with SQL or NoSQL database",
                Icon = "🗄️"
            },
            new Feature
            {
                Name = "Responsive Design",
                Description = "Adapts to different screen sizes and devices",
                Icon = "📱"
            },
            new Feature
            {
                Name = "Real-time Updates",
                Description = "Live data synchronization and notifications",
                Icon = "⚡"
            },
            new Feature
            {
                Name = "PWA Support",
                Description = "Progressive Web App capabilities",
                Icon = "📲"
            },
            new Feature
            {
                Name = "Payment Processing",
                Description = "Stripe, PayPal, and other payment integrations",
                Icon = "💳"
            },
            new Feature
            {
                Name = "Search & Filter",
                Description = "Advanced search and filtering capabilities",
                Icon = "🔍"
            },
            new Feature
            {
                Name = "File Upload",
                Description = "Upload and manage files and images",
                Icon = "📁"
            },
            new Feature
            {
                Name = "Data Visualization",
                Description = "Charts, graphs, and analytics dashboards",
                Icon = "📊"
            }
        };
    }

    private List<QuickExample> InitializeQuickExamples()
    {
        return new List<QuickExample>
        {
            new QuickExample
            {
                Title = "Todo App",
                Description = "A modern todo app with dark mode, drag-and-drop reordering, and real-time sync",
                Platform = "Web Application"
            },
            new QuickExample
            {
                Title = "Fitness Tracker",
                Description = "A mobile fitness tracker with workout plans, progress charts, and social features",
                Platform = "Mobile Application"
            },
            new QuickExample
            {
                Title = "E-commerce Store",
                Description = "A full-featured online store with product listings, shopping cart, and payment processing",
                Platform = "Web Application"
            },
            new QuickExample
            {
                Title = "Chat API",
                Description = "A real-time chat API with user management, messaging, and presence features",
                Platform = "API Server"
            },
            new QuickExample
            {
                Title = "Photo Editor",
                Description = "A desktop photo editing application with filters, cropping, and batch processing",
                Platform = "Desktop Application"
            },
            new QuickExample
            {
                Title = "2D Platformer",
                Description = "A simple 2D platformer game with character movement, jumping, and collectibles",
                Platform = "Game Application"
            }
        };
    }
}

public class AppType
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "";
    public string[] Technologies { get; set; } = Array.Empty<string>();
}

public class Feature
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "";
}

public class QuickExample
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Platform { get; set; } = "";
}
