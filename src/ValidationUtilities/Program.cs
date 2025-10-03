using System;
using System.Linq;

namespace ValidationUtilities;

public class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            ShowUsage();
            return 1;
        }

        var command = args[0].ToLowerInvariant();
        var commandArgs = args.Skip(1).ToArray();

        return command switch
        {
            "aggregate-junit" or "junit" => JUnitAggregator.Run(commandArgs),
            "check-promotion" or "promotion" => PromotionChecker.Run(commandArgs),
            "help" or "--help" or "-h" => ShowUsage(),
            _ => ShowUsage()
        };
    }

    private static int ShowUsage()
    {
        Console.WriteLine("ValidationUtilities - Cross-platform validation utilities");
        Console.WriteLine();
        Console.WriteLine("Usage: ValidationUtilities <command> [args...]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  aggregate-junit [output.xml] [input1.xml] [input2.xml] ...");
        Console.WriteLine("    Aggregate multiple JUnit XML files into one report");
        Console.WriteLine("    Default output: playmode-review-aggregate.junit.xml");
        Console.WriteLine("    Default inputs: Artifacts/*/playmode-smoke.junit.xml");
        Console.WriteLine();
        Console.WriteLine("  check-promotion");
        Console.WriteLine("    Verify Artifacts/last_good/ has all required phase outputs");
        Console.WriteLine();
        Console.WriteLine("  help");
        Console.WriteLine("    Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  ValidationUtilities aggregate-junit");
        Console.WriteLine("  ValidationUtilities aggregate-junit report.xml file1.xml file2.xml");
        Console.WriteLine("  ValidationUtilities check-promotion");
        return 0;
    }
}
