using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Nexo.API.Endpoints;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.API;

public sealed class DirectorEndpointsTests
{
    [Fact]
    public async Task RunDirectorWorkflow_WhenOrchestratorFails_ReturnsDailyAndPersistsEntry()
    {
        var dailiesPath = CreateTempDirectory();
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["NEXO_DAILIES_PATH"] = dailiesPath
                })
                .Build();

            var request = new DirectorRunRequest(
                Goal: "Generate a daily from API test",
                RunValidation: false);

            var method = GetHandler("RunDirectorWorkflowAsync");
            var task = (Task<IResult>)method.Invoke(
                null,
                [request, null, null, configuration, CancellationToken.None])!;
            var result = await task;

            var response = ExtractOkValue<DirectorRunResponse>(result);
            response.Should().NotBeNull();
            response.Success.Should().BeFalse("null orchestrator should be handled and persisted as a failed daily");
            response.DailyId.Should().NotBeNullOrWhiteSpace();
            response.OrchestrationError.Should().NotBeNullOrWhiteSpace();
            File.Exists(response.DailyPath).Should().BeTrue("daily JSON should be persisted to configured path");

            var json = await File.ReadAllTextAsync(response.DailyPath);
            json.Should().Contain(response.DailyId);
        }
        finally
        {
            TryDeleteDirectory(dailiesPath);
        }
    }

    [Fact]
    public async Task ListAndGetDailies_ReturnPersistedEntries()
    {
        var dailiesPath = CreateTempDirectory();
        try
        {
            var older = new DirectorDailyEntry(
                DailyId: "20260101000000001-older",
                CreatedAtUtc: DateTimeOffset.UtcNow.AddMinutes(-5),
                Goal: "Older goal",
                Notes: null,
                ContinueFromDailyId: null,
                Success: true,
                Summary: "older",
                IntegratedOutputJson: null,
                Validation: null,
                OrchestrationError: null);

            var newer = new DirectorDailyEntry(
                DailyId: "20260101000000002-newer",
                CreatedAtUtc: DateTimeOffset.UtcNow,
                Goal: "Newer goal",
                Notes: "note",
                ContinueFromDailyId: older.DailyId,
                Success: false,
                Summary: "newer",
                IntegratedOutputJson: null,
                Validation: null,
                OrchestrationError: "error");

            await File.WriteAllTextAsync(Path.Combine(dailiesPath, $"{older.DailyId}.json"), System.Text.Json.JsonSerializer.Serialize(older));
            await File.WriteAllTextAsync(Path.Combine(dailiesPath, $"{newer.DailyId}.json"), System.Text.Json.JsonSerializer.Serialize(newer));

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["NEXO_DAILIES_PATH"] = dailiesPath
                })
                .Build();

            var listMethod = GetHandler("ListDailiesAsync");
            var listTask = (Task<IResult>)listMethod.Invoke(null, [configuration, CancellationToken.None])!;
            var listResult = await listTask;
            var summaries = ExtractOkValue<IEnumerable<DirectorDailySummary>>(listResult).ToList();

            summaries.Should().HaveCount(2);
            summaries[0].DailyId.Should().Be(newer.DailyId, "dailies should be returned newest first");
            summaries[1].DailyId.Should().Be(older.DailyId);

            var getMethod = GetHandler("GetDailyAsync");
            var getTask = (Task<IResult>)getMethod.Invoke(null, [newer.DailyId, configuration, CancellationToken.None])!;
            var getResult = await getTask;
            var loaded = ExtractOkValue<DirectorDailyEntry>(getResult);
            loaded.DailyId.Should().Be(newer.DailyId);
            loaded.ContinueFromDailyId.Should().Be(older.DailyId);
        }
        finally
        {
            TryDeleteDirectory(dailiesPath);
        }
    }

    private static MethodInfo GetHandler(string name)
    {
        var method = typeof(NexoEndpoints).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull($"handler {name} should exist");
        return method!;
    }

    private static T ExtractOkValue<T>(IResult result)
    {
        var valueProp = result.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
        valueProp.Should().NotBeNull();
        var value = valueProp!.GetValue(result);
        value.Should().BeAssignableTo<T>();
        return (T)value!;
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nexo-director-endpoint-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Best-effort cleanup for test temp paths.
        }
    }
}
