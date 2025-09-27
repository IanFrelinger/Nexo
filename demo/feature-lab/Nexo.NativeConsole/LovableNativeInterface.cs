using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.NativeConsole;

/// <summary>
/// Lovable-style native interface for application generation
/// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
/// </summary>
public partial class LovableNativeInterface
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
    // This class acts as an orchestrator for various application generation functionalities,
    // with specific categories defined in partial classes.
}