using System;
using System.IO;

namespace ValidationUtilities;

public class PromotionChecker
{
    public static int Run(string[] args)
    {
        try
        {
            var lastGoodDir = "Artifacts/last_good";
            
            if (!Directory.Exists(lastGoodDir))
            {
                Console.WriteLine("No last_good yet");
                return 1;
            }

            var requiredPhases = new[] { "Plan", "Layout", "Placement", "Content", "Validate" };
            
            foreach (var phase in requiredPhases)
            {
                var outputFile = Path.Combine(lastGoodDir, phase, "output.json");
                if (!File.Exists(outputFile) || new FileInfo(outputFile).Length == 0)
                {
                    Console.WriteLine($"Missing last_good for {phase}");
                    return 2;
                }
            }

            Console.WriteLine("last_good complete");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
