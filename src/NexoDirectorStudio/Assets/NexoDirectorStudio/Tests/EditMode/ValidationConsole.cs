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
            Console.WriteLine("🎮 Director Studio Validation Console");
            Console.WriteLine("====================================");
            Console.WriteLine();
            
            try
            {
                var allPassed = await ValidationTestRunner.RunAllValidations();
                
                if (allPassed)
                {
                    Console.WriteLine("\n🎉 All validations passed! Director Studio is ready to use.");
                    return 0;
                }
                else
                {
                    Console.WriteLine("\n❌ Some validations failed. Please check the output above.");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n💥 Validation failed with exception: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return 1;
            }
        }
    }
}
