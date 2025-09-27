using System;
using System.Collections.Generic;

namespace Nexo.NativeConsole;

/// <summary>
/// Display and information functionality
/// </summary>
public partial class LovableNativeInterface
{
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
}
