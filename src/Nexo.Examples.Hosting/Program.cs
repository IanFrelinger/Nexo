using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nexo.Hosting;

// Minimal host: register Nexo kernel and run validation programmatically.
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddNexo();
    })
    .Build();

var validationService = host.Services.GetRequiredService<Nexo.Core.Application.Validation.Ports.IValidationService>();
var result = await validationService.ValidateAsync(
    filter: null,
    progress: null,
    CancellationToken.None);

Console.WriteLine($"Validation: {(result.Passed ? "Passed" : "Failed")} ({result.TestsRun} tests, {result.TestsPassed} passed, {result.TestsFailed} failed)");
return result.Passed ? 0 : 1;
