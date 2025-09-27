using System;
using System.Threading.Tasks;

namespace Nexo.NativeConsole;

/// <summary>
/// UI and menu handling functionality
/// </summary>
public partial class LovableNativeInterface
{
    private void ShowWelcomeScreen()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                                                                              ║");
        Console.WriteLine("║  NEXO AI DEVELOPMENT PLATFORM - NATIVE INTERFACE                            ║");
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
        Console.WriteLine("MAIN MENU");
        Console.WriteLine("============");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("1. Generate New Application");
        Console.WriteLine("2. Quick Examples");
        Console.WriteLine("3. Available Platforms");
        Console.WriteLine("4. Features & Capabilities");
        Console.WriteLine("5. Help & Documentation");
        Console.WriteLine("6. Exit");
        Console.WriteLine();
        Console.Write("Choose an option (1-6): ");
    }

    private string GetUserChoice()
    {
        return Console.ReadLine()?.Trim().ToLower() ?? "";
    }

    private void ShowInvalidChoice()
    {
        Console.WriteLine();
        Console.WriteLine("Invalid choice! Please select 1-6.");
    }

    private void ShowGoodbye()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Thank you for using Nexo AI Development Platform!");
        Console.WriteLine("Happy coding!");
        Console.ResetColor();
    }
}
