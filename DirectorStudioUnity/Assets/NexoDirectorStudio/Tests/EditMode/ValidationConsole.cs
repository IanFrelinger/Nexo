using System;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Threading.Tasks;
using NexoDirectorStudio.Tests.EditMode;

namespace NexoDirectorStudio.ValidationConsole
{
    /// <summary>
    /// Console application to run Director Studio validation tests.
    /// This can be executed to verify all components are working correctly.
    /// </summary>
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            System.Console.WriteLine("🎮 Director Studio Validation Console");
            System.Console.WriteLine("====================================");
            System.Console.WriteLine();
            
            try
            {
                var allPassed = await ValidationTestRunner.RunAllValidations();
                
                if (allPassed)
                {
                    System.Console.WriteLine("\n🎉 All validations passed! Director Studio is ready to use.");
                    return 0;
                }
                else
                {
                    System.Console.WriteLine("\n❌ Some validations failed. Please check the output above.");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\n💥 Validation failed with exception: {ex.Message}");
                System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return 1;
            }
        }
    }
}
