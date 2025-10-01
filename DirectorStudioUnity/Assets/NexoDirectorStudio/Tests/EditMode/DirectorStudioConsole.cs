using System;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Threading.Tasks;
using NexoDirectorStudio.Tests.EditMode;

namespace NexoDirectorStudio.Console
{
    /// <summary>
    /// Console application to run Director Studio interactive demos.
    /// This simulates the actual user experience of using Director Studio.
    /// </summary>
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            System.Console.WriteLine("🎮 Director Studio Console");
            System.Console.WriteLine("==========================");
            System.Console.WriteLine();
            System.Console.WriteLine("Welcome to Director Studio!");
            System.Console.WriteLine("This console simulates the Director Studio experience.");
            System.Console.WriteLine();
            
            try
            {
                if (args.Length > 0 && args[0].ToLower() == "doom")
                {
                    System.Console.WriteLine("🎮 Generating Doom-style FPS game...");
                    System.Console.WriteLine();
                    await DirectorStudioInteractiveDemo.RunDoomStyleGameGeneration();
                }
                else
                {
                    System.Console.WriteLine("Available commands:");
                    System.Console.WriteLine("  doom    - Generate a Doom-style FPS game");
                    System.Console.WriteLine();
                    System.Console.WriteLine("Usage: dotnet run doom");
                    System.Console.WriteLine();
                    System.Console.WriteLine("Example: dotnet run doom");
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Error: {ex.Message}");
                System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return 1;
            }
        }
    }
}
