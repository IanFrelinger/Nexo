#!/usr/bin/env dotnet-script
// Cross-platform test runner wrapper script
// Usage: ./scripts/run-tests.cs [options]

#r "nuget: System.CommandLine, 2.0.0-beta4.22272.1"

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

var projectRoot = GetProjectRoot();
var testRunnerProject = Path.Combine(projectRoot, "src", "Nexo.Tools.TestRunner", "Nexo.Tools.TestRunner.csproj");

if (!File.Exists(testRunnerProject))
{
    Console.WriteLine($"❌ Test runner project not found: {testRunnerProject}");
    Environment.Exit(1);
}

var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
var processStartInfo = new ProcessStartInfo
{
    FileName = "dotnet",
    Arguments = $"run --project {testRunnerProject} -- {string.Join(" ", args)}",
    WorkingDirectory = projectRoot,
    UseShellExecute = false
};

using var process = Process.Start(processStartInfo);
if (process == null)
{
    Console.WriteLine("❌ Failed to start test runner");
    Environment.Exit(1);
}

await process.WaitForExitAsync();
Environment.Exit(process.ExitCode);

static string GetProjectRoot()
{
    var current = Directory.GetCurrentDirectory();
    var dir = new DirectoryInfo(current);

    while (dir != null)
    {
        if (File.Exists(Path.Combine(dir.FullName, "src", "Nexo.CLI", "Nexo.CLI.csproj")))
        {
            return dir.FullName;
        }
        dir = dir.Parent;
    }

    return current;
}

