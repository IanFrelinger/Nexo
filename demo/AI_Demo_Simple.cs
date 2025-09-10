using System;
using System.Threading.Tasks;

namespace Nexo.AI.Demo
{
    /// <summary>
    /// Simple demo showing AI integration capabilities
    /// </summary>
    public class AIDemoSimple
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🤖 Nexo AI Integration Demo");
            Console.WriteLine("=============================");
            Console.WriteLine();

            // Simulate AI code generation
            await DemonstrateAICodeGeneration();
            
            // Simulate AI code review
            await DemonstrateAICodeReview();
            
            // Simulate AI optimization
            await DemonstrateAIOptimization();
            
            Console.WriteLine();
            Console.WriteLine("✅ AI Integration Demo Complete!");
        }

        private static async Task DemonstrateAICodeGeneration()
        {
            Console.WriteLine("📝 AI Code Generation Demo");
            Console.WriteLine("---------------------------");
            
            var prompt = "Create a simple calculator class in C#";
            Console.WriteLine($"Prompt: {prompt}");
            Console.WriteLine();
            
            // Simulate AI processing
            await Task.Delay(1000);
            
            var generatedCode = @"
public class Calculator
{
    public double Add(double a, double b) => a + b;
    public double Subtract(double a, double b) => a - b;
    public double Multiply(double a, double b) => a * b;
    public double Divide(double a, double b) => b != 0 ? a / b : throw new DivideByZeroException();
    
    public double Calculate(string operation, double a, double b)
    {
        return operation.ToLower() switch
        {
            ""add"" or ""+"" => Add(a, b),
            ""subtract"" or ""-"" => Subtract(a, b),
            ""multiply"" or ""*"" => Multiply(a, b),
            ""divide"" or ""/"" => Divide(a, b),
            _ => throw new ArgumentException($""Unknown operation: {operation}"")
        };
    }
}";
            
            Console.WriteLine("Generated Code:");
            Console.WriteLine(generatedCode);
            Console.WriteLine();
        }

        private static async Task DemonstrateAICodeReview()
        {
            Console.WriteLine("🔍 AI Code Review Demo");
            Console.WriteLine("----------------------");
            
            var code = @"
public class UserService
{
    public User GetUser(int id)
    {
        // TODO: Add null check
        return _users.FirstOrDefault(u => u.Id == id);
    }
}";
            
            Console.WriteLine("Code to Review:");
            Console.WriteLine(code);
            Console.WriteLine();
            
            // Simulate AI processing
            await Task.Delay(800);
            
            Console.WriteLine("AI Review Results:");
            Console.WriteLine("• Issue: Missing null check for _users collection");
            Console.WriteLine("• Suggestion: Add null check before LINQ operation");
            Console.WriteLine("• Suggestion: Consider using async/await for database operations");
            Console.WriteLine("• Quality Score: 7/10");
            Console.WriteLine();
        }

        private static async Task DemonstrateAIOptimization()
        {
            Console.WriteLine("⚡ AI Code Optimization Demo");
            Console.WriteLine("----------------------------");
            
            var originalCode = @"
public List<string> ProcessItems(List<string> items)
{
    var result = new List<string>();
    foreach (var item in items)
    {
        if (item != null && item.Length > 0)
        {
            result.Add(item.ToUpper());
        }
    }
    return result;
}";
            
            Console.WriteLine("Original Code:");
            Console.WriteLine(originalCode);
            Console.WriteLine();
            
            // Simulate AI processing
            await Task.Delay(1200);
            
            var optimizedCode = @"
public List<string> ProcessItems(List<string> items)
{
    return items?
        .Where(item => !string.IsNullOrEmpty(item))
        .Select(item => item.ToUpper())
        .ToList() ?? new List<string>();
}";
            
            Console.WriteLine("Optimized Code:");
            Console.WriteLine(optimizedCode);
            Console.WriteLine();
            Console.WriteLine("Improvements:");
            Console.WriteLine("• Uses LINQ for better readability");
            Console.WriteLine("• Handles null collection gracefully");
            Console.WriteLine("• More functional programming style");
            Console.WriteLine("• Performance improvement: ~15%");
            Console.WriteLine();
        }
    }
}
