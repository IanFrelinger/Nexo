using System;
using System.Threading.Tasks;

namespace Director.Tests;

/// <summary>
/// Main test runner for all Director Studio tests
/// </summary>
public class TestRunner
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("Director Studio Test Suite");
        Console.WriteLine("=========================");
        Console.WriteLine();

        if (args.Length == 0)
        {
            Console.WriteLine("Available tests:");
            Console.WriteLine("  ipc          - Test IPC communication");
            Console.WriteLine("  nexo         - Test Nexo integration");
            Console.WriteLine("  e2e          - Run end-to-end tests");
            Console.WriteLine("  unity        - Test Unity project setup");
            Console.WriteLine("  all          - Run all tests");
            Console.WriteLine();
            Console.WriteLine("Usage: dotnet run -- <test_name>");
            return 0;
        }

        var testName = args[0].ToLower();
        var success = false;

        try
        {
            switch (testName)
            {
                case "ipc":
                    Console.WriteLine("Running IPC Communication Test...");
                    var ipcTest = new IpcCommunicationTest();
                    success = await ipcTest.RunTestAsync();
                    break;

                case "nexo":
                    Console.WriteLine("Running Nexo Integration Test...");
                    var nexoTest = new NexoIntegrationTest();
                    success = await nexoTest.RunFullTestAsync();
                    break;

                case "e2e":
                    Console.WriteLine("Running End-to-End Test...");
                    var e2eTest = new EndToEndTest();
                    success = await e2eTest.RunAllTestsAsync();
                    break;

                case "unity":
                    Console.WriteLine("Running Unity Project Setup Test...");
                    var unityTest = new UnityProjectSetupTest();
                    success = await unityTest.RunAllTestsAsync();
                    break;

                case "all":
                    Console.WriteLine("Running All Tests...");
                    success = await RunAllTestsAsync();
                    break;

                default:
                    Console.WriteLine($"Unknown test: {testName}");
                    Console.WriteLine("Use 'dotnet run --' to see available tests");
                    return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed with error: {ex.Message}");
            return 1;
        }

        if (success)
        {
            Console.WriteLine("\n🎉 Test completed successfully!");
            return 0;
        }
        else
        {
            Console.WriteLine("\n❌ Test failed!");
            return 1;
        }
    }

    private static async Task<bool> RunAllTestsAsync()
    {
        var tests = new (string Name, Func<Task<bool>> Test)[]
        {
            ("IPC Communication", async () => await new IpcCommunicationTest().RunTestAsync()),
            ("Nexo Integration", async () => await new NexoIntegrationTest().RunFullTestAsync()),
            ("End-to-End", async () => await new EndToEndTest().RunAllTestsAsync()),
            ("Unity Project Setup", async () => await new UnityProjectSetupTest().RunAllTestsAsync())
        };

        var allSuccess = true;
        var results = new List<(string Name, bool Success)>();

        foreach (var testInfo in tests)
        {
            Console.WriteLine($"\n{'='*50}");
            Console.WriteLine($"Running {testInfo.Name} Test");
            Console.WriteLine($"{'='*50}");

            try
            {
                var success = await testInfo.Test();
                results.Add((testInfo.Name, success));
                allSuccess = allSuccess && success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {testInfo.Name} test failed with exception: {ex.Message}");
                results.Add((testInfo.Name, false));
                allSuccess = false;
            }
        }

        // Summary
        Console.WriteLine($"\n{'='*50}");
        Console.WriteLine("TEST SUITE SUMMARY");
        Console.WriteLine($"{'='*50}");

        foreach (var (name, success) in results)
        {
            var status = success ? "✅ PASS" : "❌ FAIL";
            Console.WriteLine($"{status} {name}");
        }

        var passedCount = results.Count(r => r.Success);
        var totalCount = results.Count;
        Console.WriteLine($"\nTotal: {passedCount}/{totalCount} tests passed");

        if (allSuccess)
        {
            Console.WriteLine("\n🎉 ALL TESTS PASSED!");
            Console.WriteLine("Director Studio is ready for production use!");
        }
        else
        {
            Console.WriteLine("\n⚠️  Some tests failed. Check the output above for details.");
        }

        return allSuccess;
    }
}
