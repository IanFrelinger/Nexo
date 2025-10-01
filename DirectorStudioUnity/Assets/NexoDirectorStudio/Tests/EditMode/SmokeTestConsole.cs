using System;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Threading.Tasks;
using NexoDirectorStudio.Tests.EditMode;

namespace NexoDirectorStudio.SmokeTestConsole
{
    /// <summary>
    /// Console application to run Director Studio smoke tests.
    /// This validates that all refactored components work correctly.
    /// </summary>
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            System.Console.WriteLine("🎮 Director Studio Smoke Test Console");
            System.Console.WriteLine("====================================");
            System.Console.WriteLine();
            
            try
            {
                var allPassed = await SmokeTestRunner.RunSmokeTests();
                
                if (allPassed)
                {
                    System.Console.WriteLine("\n🎉 All smoke tests passed! Director Studio is working correctly.");
                    return 0;
                }
                else
                {
                    System.Console.WriteLine("\n❌ Some smoke tests failed. Please check the output above.");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\n💥 Smoke test failed with exception: {ex.Message}");
                System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return 1;
            }
        }
    }
}
