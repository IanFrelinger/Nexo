using System;
using System.IO;
using FluentAssertions;
using Xunit.Abstractions;

namespace Nexo.Tests.Shared.Infrastructure;

/// <summary>
/// Base class providing common test infrastructure and helper methods.
/// Consolidates duplicate functionality across all test classes.
/// </summary>
public abstract class TestBase
{
    protected readonly ITestOutputHelper? Output;

    protected TestBase(ITestOutputHelper? output = null)
    {
        Output = output;
    }

    /// <summary>
    /// Gets the project root directory by traversing up from current directory
    /// until finding Nexo.sln file.
    /// </summary>
    protected static string GetProjectRoot()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var directory = new DirectoryInfo(currentDirectory);
        
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Nexo.sln")))
        {
            directory = directory.Parent;
        }
        
        return directory?.FullName ?? throw new InvalidOperationException("Could not find project root");
    }

    /// <summary>
    /// Asserts that a file exists at the specified path.
    /// </summary>
    protected static void AssertFileExists(string filePath, string? customMessage = null)
    {
        var message = customMessage ?? $"File should exist at {filePath}";
        File.Exists(filePath).Should().BeTrue(message);
    }

    /// <summary>
    /// Asserts that a directory exists at the specified path.
    /// </summary>
    protected static void AssertDirectoryExists(string directoryPath, string? customMessage = null)
    {
        var message = customMessage ?? $"Directory should exist at {directoryPath}";
        Directory.Exists(directoryPath).Should().BeTrue(message);
    }

    /// <summary>
    /// Asserts that file content contains expected text.
    /// </summary>
    protected static void AssertContentContains(string filePath, string expectedContent, string? customMessage = null)
    {
        var content = File.ReadAllText(filePath);
        var message = customMessage ?? $"File content should contain '{expectedContent}'";
        content.Should().Contain(expectedContent, message);
    }

    /// <summary>
    /// Asserts that file content contains all expected texts.
    /// </summary>
    protected static void AssertContentContainsAll(string filePath, string[] expectedContents, string? customMessage = null)
    {
        var content = File.ReadAllText(filePath);
        foreach (var expected in expectedContents)
        {
            var message = customMessage ?? $"File content should contain '{expected}'";
            content.Should().Contain(expected, message);
        }
    }

    /// <summary>
    /// Asserts that multiple files exist.
    /// </summary>
    protected static void AssertFilesExist(string[] filePaths, string? customMessage = null)
    {
        foreach (var filePath in filePaths)
        {
            AssertFileExists(filePath, customMessage);
        }
    }

    /// <summary>
    /// Asserts that multiple directories exist.
    /// </summary>
    protected static void AssertDirectoriesExist(string[] directoryPaths, string? customMessage = null)
    {
        foreach (var directoryPath in directoryPaths)
        {
            AssertDirectoryExists(directoryPath, customMessage);
        }
    }

    /// <summary>
    /// Logs a message to test output if available.
    /// </summary>
    protected void LogMessage(string message)
    {
        Output?.WriteLine(message);
    }
}
