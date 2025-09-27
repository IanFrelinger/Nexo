using System;

namespace FeatureFactoryDemo.Commands
{
    /// <summary>
    /// Argument parsing functionality for E2E generation command.
    /// </summary>
    public partial class GenerateWithE2ECommand
    {
        private (string description, string platform, int targetScore, int maxIterations) ParseArguments(string[] args)
        {
            string description = string.Empty;
            string platform = string.Empty;
            int targetScore = 90;
            int maxIterations = 20;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--description" when i + 1 < args.Length:
                        description = args[i + 1];
                        i++;
                        break;
                    case "--platform" when i + 1 < args.Length:
                        platform = args[i + 1];
                        i++;
                        break;
                    case "--target-score" when i + 1 < args.Length:
                        if (int.TryParse(args[i + 1], out int score))
                            targetScore = score;
                        i++;
                        break;
                    case "--max-iterations" when i + 1 < args.Length:
                        if (int.TryParse(args[i + 1], out int iterations))
                            maxIterations = iterations;
                        i++;
                        break;
                }
            }

            return (description, platform, targetScore, maxIterations);
        }
    }
}
