using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces;

namespace Nexo.Tests.E2E;

/// <summary>
/// Real assembly analysis command for E2E testing
/// </summary>
public class AssemblyAnalysisCommand : ICommand<AssemblyAnalysisRequest, AssemblyAnalysisResult>
{
    public ValueTask<AssemblyAnalysisResult> ExecuteAsync(AssemblyAnalysisRequest input, CancellationToken ct)
    {
        try
        {
            if (!File.Exists(input.AssemblyPath))
            {
                return ValueTask.FromResult(new AssemblyAnalysisResult
                {
                    Success = false,
                    ErrorMessage = $"Assembly not found: {input.AssemblyPath}"
                });
            }

            var assemblyName = AssemblyName.GetAssemblyName(input.AssemblyPath);
            var assembly = Assembly.LoadFrom(input.AssemblyPath);
            var types = assembly.GetTypes();

            var result = new AssemblyAnalysisResult
            {
                Success = true,
                AssemblyName = assemblyName.Name ?? "Unknown",
                Version = assemblyName.Version?.ToString() ?? "Unknown",
                TypeCount = types.Length,
                TargetPath = input.AssemblyPath,
                AnalysisTimestamp = DateTime.UtcNow
            };

            return ValueTask.FromResult(result);
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult(new AssemblyAnalysisResult
            {
                Success = false,
                ErrorMessage = $"Analysis failed: {ex.Message}",
                TargetPath = input.AssemblyPath
            });
        }
    }
}

public record AssemblyAnalysisRequest(string AssemblyPath);

public record AssemblyAnalysisResult
{
    public bool Success { get; init; }
    public string? AssemblyName { get; init; }
    public string? Version { get; init; }
    public int TypeCount { get; init; }
    public string? TargetPath { get; init; }
    public DateTime AnalysisTimestamp { get; init; }
    public string? ErrorMessage { get; init; }
}
