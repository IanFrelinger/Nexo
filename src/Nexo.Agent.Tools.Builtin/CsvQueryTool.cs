using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Nexo.Agent.Models;
using Nexo.Agent.Abstractions;

namespace Nexo.Agent.Tools.Builtin;

/// <summary>
/// Tool for querying CSV files with simple operations.
/// </summary>
public partial class CsvQueryTool : ITool<CsvQueryInput, CsvQueryOutput>
{
    public ToolManifest Manifest { get; }

    public CsvQueryTool()
    {
        Manifest = ToolManifest.Create(
            id: "tool.csv.query",
            name: "CSV Query",
            description: "Query CSV files with simple operations like count, select, and filter",
            requiredPermissions: ToolPermissions.FileRead,
            capabilities: new[] { "csv", "query", "data", "analysis" },
            timeout: TimeSpan.FromMinutes(5)
        );
    }

    public async Task<string> ExecuteAsync(string input, ToolContext context, CancellationToken cancellationToken = default)
    {
        var typedInput = JsonSerializer.Deserialize<CsvQueryInput>(input) ?? throw new ArgumentException("Invalid input");
        var result = await ExecuteAsync(typedInput, context, cancellationToken);
        return JsonSerializer.Serialize(result);
    }

    public async Task<CsvQueryOutput> ExecuteAsync(CsvQueryInput input, ToolContext context, CancellationToken cancellationToken = default)
    {
        using var activity = new ActivitySource("Nexo.Tool").StartActivity("CsvQuery");
        activity?.SetTag("tool.csv.file_path", input.FilePath);
        activity?.SetTag("tool.csv.operation", input.Operation);

        try
        {
            context.Logger.LogInformation("Querying CSV file: {FilePath} with operation: {Operation}", input.FilePath, input.Operation);

            // Check if file exists
            if (!context.FileSystem.FileExists(input.FilePath))
            {
                var error = $"CSV file not found: {input.FilePath}";
                context.Logger.LogError(error);
                activity?.SetStatus(ActivityStatusCode.Error, error);
                return new CsvQueryOutput(false, null, error, 0, 0, Array.Empty<Dictionary<string, string>>());
            }

            // Read CSV content
            var csvContent = await context.FileSystem.ReadAllTextAsync(input.FilePath, cancellationToken);
            var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            if (lines.Length == 0)
            {
                return new CsvQueryOutput(true, "Empty CSV file", null, 0, 0, Array.Empty<Dictionary<string, string>>());
            }

            // Parse header
            var header = ParseCsvLine(lines[0]);
            var dataRows = lines.Skip(1).Select(ParseCsvLine).ToList();

            context.Logger.LogInformation("CSV file has {RowCount} rows and {ColumnCount} columns", dataRows.Count, header.Length);

            // Execute query based on operation
            var result = input.Operation.ToLowerInvariant() switch
            {
                "count" => ExecuteCountQuery(dataRows, input),
                "select" => ExecuteSelectQuery(header, dataRows, input),
                "filter" => ExecuteFilterQuery(header, dataRows, input),
                _ => throw new ArgumentException($"Unknown operation: {input.Operation}")
            };

            activity?.SetTag("tool.csv.result_rows", result.Rows);
            activity?.SetTag("tool.csv.result_columns", result.Columns);

            return result;
        }
        catch (Exception ex)
        {
            var error = $"Error querying CSV file {input.FilePath}: {ex.Message}";
            context.Logger.LogError(ex, error);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return new CsvQueryOutput(false, null, error, 0, 0, Array.Empty<Dictionary<string, string>>());
        }
    }

    private static string[] ParseCsvLine(string line)
    {
        // Simple CSV parsing - handles basic cases
        var result = new List<string>();
        var current = "";
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.Trim());
                current = "";
            }
            else
            {
                current += c;
            }
        }

        result.Add(current.Trim());
        return result.ToArray();
    }

    private CsvQueryOutput ExecuteCountQuery(List<string[]> dataRows, CsvQueryInput input)
    {
        var count = dataRows.Count;
        var message = $"Found {count} rows in CSV file";
        
        return new CsvQueryOutput(true, message, null, count, 0, Array.Empty<Dictionary<string, string>>());
    }

    private CsvQueryOutput ExecuteSelectQuery(string[] header, List<string[]> dataRows, CsvQueryInput input)
    {
        var selectedColumns = input.Columns?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim()).ToArray() ?? header;

        var result = new List<Dictionary<string, string>>();
        
        foreach (var row in dataRows)
        {
            var rowDict = new Dictionary<string, string>();
            for (int i = 0; i < Math.Min(header.Length, row.Length); i++)
            {
                if (selectedColumns.Contains(header[i], StringComparer.OrdinalIgnoreCase))
                {
                    rowDict[header[i]] = row[i];
                }
            }
            result.Add(rowDict);
        }

        var message = $"Selected {selectedColumns.Length} columns from {dataRows.Count} rows";
        return new CsvQueryOutput(true, message, null, dataRows.Count, selectedColumns.Length, result);
    }

    private CsvQueryOutput ExecuteFilterQuery(string[] header, List<string[]> dataRows, CsvQueryInput input)
    {
        if (string.IsNullOrEmpty(input.FilterColumn) || string.IsNullOrEmpty(input.FilterValue))
        {
            return new CsvQueryOutput(false, "Filter column and value are required for filter operation", null, 0, 0, Array.Empty<Dictionary<string, string>>());
        }

        var columnIndex = Array.FindIndex(header, h => h.Equals(input.FilterColumn, StringComparison.OrdinalIgnoreCase));
        if (columnIndex == -1)
        {
            return new CsvQueryOutput(false, $"Column '{input.FilterColumn}' not found", null, 0, 0, Array.Empty<Dictionary<string, string>>());
        }

        var filteredRows = dataRows.Where(row => 
            row.Length > columnIndex && 
            row[columnIndex].Contains(input.FilterValue, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var result = new List<Dictionary<string, string>>();
        foreach (var row in filteredRows)
        {
            var rowDict = new Dictionary<string, string>();
            for (int i = 0; i < Math.Min(header.Length, row.Length); i++)
            {
                rowDict[header[i]] = row[i];
            }
            result.Add(rowDict);
        }

        var message = $"Filtered to {filteredRows.Count} rows where {input.FilterColumn} contains '{input.FilterValue}'";
        return new CsvQueryOutput(true, message, null, filteredRows.Count, header.Length, result);
    }
}

/// <summary>
/// Input for the CsvQuery tool.
/// </summary>
/// <param name="FilePath">Path to the CSV file</param>
/// <param name="Operation">Operation to perform (count, select, filter)</param>
/// <param name="Columns">Comma-separated list of columns to select (for select operation)</param>
/// <param name="FilterColumn">Column to filter on (for filter operation)</param>
/// <param name="FilterValue">Value to filter by (for filter operation)</param>
public record CsvQueryInput(
    string FilePath,
    string Operation = "count",
    string? Columns = null,
    string? FilterColumn = null,
    string? FilterValue = null);

/// <summary>
/// Output from the CsvQuery tool.
/// </summary>
/// <param name="Success">Whether the query executed successfully</param>
/// <param name="Message">Result message</param>
/// <param name="Error">Error message if query failed</param>
/// <param name="Rows">Number of rows in the result</param>
/// <param name="Columns">Number of columns in the result</param>
/// <param name="Data">Query result data</param>
public record CsvQueryOutput(
    bool Success,
    string? Message,
    string? Error,
    int Rows,
    int Columns,
    IReadOnlyList<Dictionary<string, string>> Data);
