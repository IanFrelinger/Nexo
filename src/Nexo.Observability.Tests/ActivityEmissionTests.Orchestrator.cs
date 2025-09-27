using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Nexo.Observability.ActivitySources;
using Nexo.Observability.Metrics;
using Xunit;
        using var activityListener = new ActivityListener
        using var activity = activitySource.StartActivity("test.generation");
        using var meterListener = new MeterListener

namespace Nexo.Observability.Tests;
{
}