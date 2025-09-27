// This file has been refactored into specialized workflow components:
// - GameTesting/Core/GameTestingWorkflow.cs - Main orchestrator
// - GameTesting/Phases/*Runner.cs - Phase runners
// - GameTesting/Reporting/TestReportGenerator.cs - Report generation

// Re-export for backward compatibility
using Nexo.Feature.Unity.Workflows.GameTesting.Core;
using Nexo.Feature.Unity.Workflows.GameTesting.Phases;
using Nexo.Feature.Unity.Workflows.GameTesting.Reporting;

namespace Nexo.Feature.Unity.Workflows
{
    // All game testing workflow classes are available through specialized namespaces above.
}


