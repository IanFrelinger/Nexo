using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Enums;
using Nexo.Feature.Pipeline.Models;
using Xunit;
using Xunit.Abstractions;

namespace Nexo.Feature.Pipeline.Tests.Runtime
{
    /// <summary>
    /// Unity-specific tests for the Pipeline feature.
    /// These tests demonstrate how to test Pipeline functionality within Unity's runtime environment.
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public partial class UnityPipelineTests : CrossRuntimeTestBase
    {
        public UnityPipelineTests(ITestOutputHelper testOutput) : base(testOutput)
        {
        }
    }
}