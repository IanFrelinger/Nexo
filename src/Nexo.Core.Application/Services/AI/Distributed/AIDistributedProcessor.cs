// This file has been refactored into specialized distributed processing classes:
// - Core/AIDistributedProcessor.cs - Main distributed processor orchestrator
// - Processing/NodeManager.cs - Node management
// - Processing/TaskProcessor.cs - Task and sub-task processing
// - Statistics/StatisticsCollector.cs - Statistics aggregation
// - Models/* - Node/Task/Statistics models and enums

// Re-export for backward compatibility
using Nexo.Core.Application.Services.AI.Distributed.Core;
using Nexo.Core.Application.Services.AI.Distributed.Models;
using Nexo.Core.Application.Services.AI.Distributed.Processing;
using Nexo.Core.Application.Services.AI.Distributed.Statistics;

namespace Nexo.Core.Application.Services.AI.Distributed
{
    // All distributed processing classes are now available via the specialized namespaces above.
}
