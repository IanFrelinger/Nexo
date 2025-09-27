using System;
using System.Linq;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Display and summary functionality
    /// </summary>
    public partial class PolicyCommands
    {
        private void DisplaySafetySummary(Nexo.Core.Domain.Models.Policy.SafetyPolicyResult result)
        {
            Console.WriteLine();
            Console.WriteLine("Security Safety Scan Summary");
            Console.WriteLine("=====================");
            Console.WriteLine($"Status: {(result.Passed ? "SUCCESS: PASSED" : "ERROR: FAILED")}");
            Console.WriteLine($"Safety Score: {result.SafetyScore:F1}/10.0");
            Console.WriteLine($"Violations: {result.Violations.Count}");

            if (result.Violations.Any())
            {
                Console.WriteLine();
                Console.WriteLine("Violations:");
                foreach (var violation in result.Violations)
                {
                    var icon = violation.Severity switch
                    {
                        "block" => "BLOCKED",
                        "error" => "ERROR:",
                        "warn" => "WARNING:",
                        "info" => "INFO:",
                        _ => "•"
                    };
                    Console.WriteLine($"  {icon} [{violation.Severity.ToUpper()}] {violation.Description}");
                    Console.WriteLine($"      {violation.Message}");
                }
            }
        }

        private void DisplayQualitySummary(Nexo.Core.Domain.Models.Policy.QualityPolicyResult result)
        {
            Console.WriteLine();
            Console.WriteLine("Stats Quality Validation Summary");
            Console.WriteLine("=============================");
            Console.WriteLine($"Status: {(result.Passed ? "SUCCESS: PASSED" : "ERROR: FAILED")}");
            Console.WriteLine($"Quality Score: {result.QualityScore:F2}");
            Console.WriteLine($"Violations: {result.Violations.Count}");

            if (result.GateScores.Any())
            {
                Console.WriteLine();
                Console.WriteLine("Gate Scores:");
                foreach (var gate in result.GateScores)
                {
                    Console.WriteLine($"  {gate.Key}: {gate.Value:P1}");
                }
            }

            if (result.Violations.Any())
            {
                Console.WriteLine();
                Console.WriteLine("Violations:");
                foreach (var violation in result.Violations)
                {
                    var icon = violation.Severity switch
                    {
                        "error" => "ERROR:",
                        "warn" => "WARNING:",
                        "info" => "INFO:",
                        _ => "•"
                    };
                    Console.WriteLine($"  {icon} [{violation.Severity.ToUpper()}] {violation.Description}");
                    Console.WriteLine($"      {violation.Message}");
                }
            }
        }

        private void DisplayPolicySummary(Nexo.Core.Domain.Models.Policy.PolicyExecutionResult result)
        {
            Console.WriteLine();
            Console.WriteLine("List Policy Execution Summary");
            Console.WriteLine("============================");
            Console.WriteLine($"Status: {(result.Passed ? "SUCCESS: PASSED" : "ERROR: FAILED")}");
            Console.WriteLine($"Report: {result.ReportPath}");

            if (result.SafetyResult != null)
            {
                Console.WriteLine($"Safety Score: {result.SafetyResult.SafetyScore:F1}/10.0");
            }

            if (result.QualityResult != null)
            {
                Console.WriteLine($"Quality Score: {result.QualityResult.QualityScore:F2}");
            }

            if (result.Errors.Any())
            {
                Console.WriteLine();
                Console.WriteLine("Errors:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  ERROR: {error}");
                }
            }
        }
    }
}
