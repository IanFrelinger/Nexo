#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.Extensions.Logging, 8.0.0"
#r "nuget: Microsoft.Extensions.Logging.Console, 8.0.0"

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Infrastructure.Testing;

// Simple test runner for agent smoke tests
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger("AgentSmokeTests");

logger.LogInformation("Running Agent Smoke Tests");
logger.LogInformation("==========================");

var testRunner = new TestRunnerAdapter(logger, null);

try
{
    var result = await testRunner.RunTestsAsync("AgentSmokeTests", null, CancellationToken.None);
    
    logger.LogInformation("");
    logger.LogInformation("Test Results:");
    logger.LogInformation("  Total Tests: {Total}", result.TotalTests);
    logger.LogInformation("  Passed: {Passed}", result.PassedTests);
    logger.LogInformation("  Failed: {Failed}", result.FailedTests);
    logger.LogInformation("  Duration: {Duration}ms", result.Duration.TotalMilliseconds);
    
    if (result.Results != null)
    {
        foreach (var testResult in result.Results)
        {
            var status = testResult.Passed ? "✓" : "✗";
            logger.LogInformation("  {Status} {TestName} ({Category})", status, testResult.TestName, testResult.Category);
            if (!testResult.Passed && !string.IsNullOrEmpty(testResult.ErrorMessage))
            {
                logger.LogError("    Error: {Error}", testResult.ErrorMessage);
            }
        }
    }
    
    Environment.Exit(result.FailedTests > 0 ? 1 : 0);
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to run tests");
    Environment.Exit(1);
}
