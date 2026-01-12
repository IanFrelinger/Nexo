#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.Extensions.Logging, 8.0.0"
#r "nuget: Microsoft.Extensions.Logging.Console, 8.0.0"
#r "nuget: Microsoft.Extensions.DependencyInjection, 8.0.0"

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Testing.Ports;
using Nexo.Core.Application.Testing.Models;
using Nexo.Infrastructure.Testing;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
var logger = loggerFactory.CreateLogger("AgentSmokeTests");

logger.LogInformation("========================================");
logger.LogInformation("Running Agent Smoke Tests");
logger.LogInformation("========================================");
logger.LogInformation("");

var serviceProvider = new ServiceCollection()
    .AddSingleton<ILoggerFactory>(loggerFactory)
    .AddSingleton(loggerFactory.CreateLogger<TestRunnerAdapter>())
    .AddSingleton<ITestRunner, TestRunnerAdapter>()
    .BuildServiceProvider();

var testRunner = serviceProvider.GetRequiredService<ITestRunner>();

try
{
    var result = await testRunner.RunTestsAsync("AgentSmokeTests", null, CancellationToken.None);
    
    logger.LogInformation("");
    logger.LogInformation("========================================");
    logger.LogInformation("Test Results Summary");
    logger.LogInformation("========================================");
    logger.LogInformation("Total Tests: {Total}", result.TotalTests);
    logger.LogInformation("Passed: {Passed}", result.PassedTests);
    logger.LogInformation("Failed: {Failed}", result.FailedTests);
    logger.LogInformation("Duration: {Duration}ms", result.Duration.TotalMilliseconds);
    logger.LogInformation("");
    
    if (result.Results != null && result.Results.Count > 0)
    {
        logger.LogInformation("Individual Test Results:");
        logger.LogInformation("----------------------------------------");
        foreach (var testResult in result.Results)
        {
            var status = testResult.Passed ? "✓ PASS" : "✗ FAIL";
            var color = testResult.Passed ? ConsoleColor.Green : ConsoleColor.Red;
            
            Console.ForegroundColor = color;
            logger.LogInformation("{Status} {TestName} ({Category})", status, testResult.TestName, testResult.Category);
            Console.ResetColor();
            
            if (!string.IsNullOrEmpty(testResult.Message))
            {
                logger.LogInformation("  Message: {Message}", testResult.Message);
            }
            
            if (!testResult.Passed && !string.IsNullOrEmpty(testResult.ErrorMessage))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                logger.LogError("  Error: {Error}", testResult.ErrorMessage);
                Console.ResetColor();
            }
        }
    }
    
    logger.LogInformation("");
    logger.LogInformation("========================================");
    
    Environment.Exit(result.FailedTests > 0 ? 1 : 0);
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to run tests");
    Environment.Exit(1);
}
