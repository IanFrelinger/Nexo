using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Observability;
using Nexo.Observability.ActivitySources;
using Nexo.Observability.Metrics;
using Xunit;
            using var activity = activitySource.StartActivity("TestGeneration");

namespace Nexo.CLI.Tests
{
}