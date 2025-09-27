using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.Observability;
using Nexo.Observability.ActivitySources;
using Nexo.Observability.Metrics;
        using var host = hostBuilder.Build();
        using var activity = _generationActivitySource.StartActivity("example.process_request");
        using var activity = _generationActivitySource.StartActivity("generation.simulate");
        using var activity = _validationActivitySource.StartActivity("validation.simulate");
        using var activity = _policyActivitySource.StartActivity("policy.simulate");

namespace Nexo.Examples;
{
}