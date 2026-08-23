using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ashlar.Client;

var baseUrl =
    Environment.GetEnvironmentVariable("ASHLAR_BASE_URL")
    ?? (args.Length > 0 ? args[0].Trim() : "http://localhost:5000");

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddAshlarClient(baseUrl);

await using var provider = services.BuildServiceProvider();
var log = provider.GetRequiredService<ILoggerFactory>().CreateLogger("demo");
var client = provider.GetRequiredService<IAshlarClient>();

log.LogInformation("Ashlar console demo — GET api/status at {BaseUrl}", baseUrl.TrimEnd('/'));

try
{
    var status = await client.GetStatusAsync().ConfigureAwait(false);
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(status, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
}
catch (Exception ex)
{
    log.LogError(ex, "Request failed (is Ashlar.API running?)");
    Environment.ExitCode = 1;
}
