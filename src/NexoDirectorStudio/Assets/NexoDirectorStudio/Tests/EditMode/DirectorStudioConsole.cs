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
            Console.WriteLine("🎮 Director Studio Console");
            Console.WriteLine("==========================");
            Console.WriteLine();
            Console.WriteLine("Welcome to Director Studio!");
            Console.WriteLine("This console simulates the Director Studio experience.");
            Console.WriteLine();
            
            try
            {
                if (args.Length > 0 && args[0].ToLower() == "doom")
                {
                    Console.WriteLine("🎮 Generating Doom-style FPS game...");
                    Console.WriteLine();
                    await DirectorStudioInteractiveDemo.RunDoomStyleGameGeneration();
                }
                else
                {
                    Console.WriteLine("Available commands:");
                    Console.WriteLine("  doom    - Generate a Doom-style FPS game");
                    Console.WriteLine();
                    Console.WriteLine("Usage: dotnet run doom");
                    Console.WriteLine();
                    Console.WriteLine("Example: dotnet run doom");
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return 1;
            }
        }
    }
}
