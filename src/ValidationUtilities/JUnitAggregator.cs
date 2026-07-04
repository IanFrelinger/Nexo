using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace ValidationUtilities;

/// <summary>J unit aggregator.</summary>
public class JUnitAggregator
{
    public static int Run(string[] args)
    {
        try
        {
            var outputFile = args.Length > 0 ? args[0] : "playmode-review-aggregate.junit.xml";
            var inputFiles = args.Length > 1 ? args.Skip(1).ToArray() : GetDefaultInputFiles();

            if (inputFiles.Length == 0)
            {
                Console.WriteLine("No JUnit files found to aggregate");
                return 1;
            }

            var result = AggregateJUnitFiles(inputFiles, outputFile);
            Console.WriteLine($"Wrote {outputFile} (tests={result.Tests}, failures={result.Failures})");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static string[] GetDefaultInputFiles()
    {
        var artifactsDir = "Artifacts";
        if (!Directory.Exists(artifactsDir))
            return Array.Empty<string>();

        return Directory.GetFiles(artifactsDir, "playmode-smoke.junit.xml", SearchOption.AllDirectories);
    }

    private static (int Tests, int Failures) AggregateJUnitFiles(string[] inputFiles, string outputFile)
    {
        var totalTests = 0;
        var totalFailures = 0;
        var totalTime = 0.0;
        var testCases = new List<string>();

        foreach (var file in inputFiles)
        {
            if (!File.Exists(file))
                continue;

            try
            {
                var doc = new XmlDocument();
                doc.Load(file);

                var tests = GetIntAttribute(doc.DocumentElement, "tests");
                var failures = GetIntAttribute(doc.DocumentElement, "failures");
                var time = GetDoubleAttribute(doc.DocumentElement, "time");

                totalTests += tests;
                totalFailures += failures;
                totalTime += time;

                // Extract test cases
                var testCaseNodes = doc.SelectNodes("//testcase");
                if (testCaseNodes != null)
                {
                    foreach (XmlNode testCase in testCaseNodes)
                    {
                        testCases.Add(testCase.OuterXml);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not process {file}: {ex.Message}");
            }
        }

        // Create aggregated XML
        var aggregatedXml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<testsuite name=""ReviewAggregate"" tests=""{totalTests}"" failures=""{totalFailures}"" time=""{totalTime:F1}"">
{string.Join("\n", testCases)}
</testsuite>";

        File.WriteAllText(outputFile, aggregatedXml);
        return (totalTests, totalFailures);
    }

    private static int GetIntAttribute(XmlElement? element, string attributeName)
    {
        if (element?.GetAttribute(attributeName) is { } value && int.TryParse(value, out var result))
            return result;
        return 0;
    }

    private static double GetDoubleAttribute(XmlElement? element, string attributeName)
    {
        if (element?.GetAttribute(attributeName) is { } value && double.TryParse(value, out var result))
            return result;
        return 0.0;
    }
}
