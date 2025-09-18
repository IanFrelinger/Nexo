using System.CommandLine;
using Nexo.Verify;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Verify command for running verification tests
    /// </summary>
    public static class VerifyCommand
    {
        /// <summary>
        /// Create the verify command
        /// </summary>
        public static Command CreateVerifyCommand()
        {
            var verifyCommand = new Command("verify", "Run verification tests to validate product claims");
            
            var specArgument = new Argument<string>("spec", "Path to .verify.yaml file or directory containing verification specs");
            var modeOption = new Option<string>("--mode", "Filter to specific mode (off, assist, hybrid, embedded)") { IsRequired = false };
            
            verifyCommand.AddArgument(specArgument);
            verifyCommand.AddOption(modeOption);
            
            verifyCommand.SetHandler(async (spec, mode) =>
            {
                try
                {
                    Console.WriteLine($"Running verification tests: {spec}");
                    if (!string.IsNullOrEmpty(mode))
                    {
                        Console.WriteLine($"Mode filter: {mode}");
                    }
                    
                    var exitCode = await VerifyRunner.RunAsync(spec);
                    Environment.Exit(exitCode);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Verification failed: {ex.Message}");
                    Environment.Exit(1);
                }
            }, specArgument, modeOption);
            
            return verifyCommand;
        }
    }
}
