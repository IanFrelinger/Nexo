using System;
using System.Linq;
using StandaloneTestRunner.Models;

namespace StandaloneTestRunner.Services
{
    public class ArgumentParserService
    {
        public TestOptions ParseArguments(string[] args)
        {
            var options = new TestOptions();

            // Parse boolean flags
            options.Discover = args.Contains("--discover");
            options.ForceTimeout = args.Contains("--force-timeout");
            options.Progress = args.Contains("--progress");
            options.Verbose = args.Contains("--verbose");
            options.Coverage = args.Contains("--coverage");
            options.SmokeTests = args.Contains("--smoke-tests");
            options.Epic54Tests = args.Contains("--epic5-4-tests");
            options.Epic54Category = args.Contains("--epic5-4-category");
            options.Epic54Priority = args.Contains("--epic5-4-priority");
            options.Epic54Enhanced = args.Contains("--epic5-4-enhanced");
            options.Epic54Phase = args.Contains("--epic5-4-phase");
            options.FeatureFactoryDomain = args.Contains("--feature-factory-domain");
            options.FeatureFactoryApplication = args.Contains("--feature-factory-application");
            options.FeatureFactoryDeployment = args.Contains("--feature-factory-deployment");
            options.AIServices = args.Contains("--ai-services");
            options.CoreDomainEntities = args.Contains("--core-domain-entities");
            options.ShowHelp = args.Contains("--help") || args.Contains("-h");

            // Parse timeout arguments
            var timeoutArg = args.FirstOrDefault(a => a.StartsWith("--timeout="));
            options.Timeout = timeoutArg != null ? int.Parse(timeoutArg.Split('=')[1]) : 5;

            var heartbeatArg = args.FirstOrDefault(a => a.StartsWith("--heartbeat-interval="));
            options.HeartbeatInterval = heartbeatArg != null ? int.Parse(heartbeatArg.Split('=')[1]) : 2;

            var processTimeoutArg = args.FirstOrDefault(a => a.StartsWith("--process-timeout="));
            options.ProcessTimeout = processTimeoutArg != null ? int.Parse(processTimeoutArg.Split('=')[1]) : 1;

            // Parse filter arguments
            var categoryArg = args.FirstOrDefault(a => a.StartsWith("--category="));
            options.Category = categoryArg?.Split('=')[1];

            var priorityArg = args.FirstOrDefault(a => a.StartsWith("--priority="));
            options.Priority = priorityArg?.Split('=')[1];

            return options;
        }

        public void DisplayParsedOptions(TestOptions options)
        {
            Console.WriteLine($"Default Timeout: {options.Timeout} seconds");
            Console.WriteLine($"Force Timeout: {(options.ForceTimeout ? "Enabled" : "Disabled")}");
            
            if (options.ForceTimeout)
            {
                Console.WriteLine($"Heartbeat Interval: {options.HeartbeatInterval} seconds");
                Console.WriteLine($"Process Timeout: {options.ProcessTimeout} minutes");
            }

            if (!string.IsNullOrEmpty(options.Category))
            {
                Console.WriteLine($"Category Filter: {options.Category}");
            }

            if (!string.IsNullOrEmpty(options.Priority))
            {
                Console.WriteLine($"Priority Filter: {options.Priority}");
            }

            Console.WriteLine($"Progress: {(options.Progress ? "Enabled" : "Disabled")}");
            Console.WriteLine($"Verbose: {(options.Verbose ? "Enabled" : "Disabled")}");
            Console.WriteLine($"Coverage: {(options.Coverage ? "Enabled" : "Disabled")}");
            Console.WriteLine();
        }
    }
}
