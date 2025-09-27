// This file has been refactored into specialized adaptation command classes:
// - Core/AdaptationCommands.cs - Main orchestrator for adaptation command functionality
// - Commands/StatusCommandHandler.cs - Status command handling
// - Commands/MonitorCommandHandler.cs - Monitor command handling
// - Commands/TriggerCommandHandler.cs - Trigger command handling
// - Commands/LearningCommandHandler.cs - Learning command handling
// - Commands/EffectivenessCommandHandler.cs - Effectiveness command handling
// - Commands/DashboardCommandHandler.cs - Dashboard command handling
// - Commands/EnvironmentCommandHandler.cs - Environment command handling
// - Commands/EngineCommandHandler.cs - Engine control command handling
// - Handlers/StatusHandler.cs - Status display functionality
// - Handlers/MonitorHandler.cs - Monitoring functionality
// - Handlers/TriggerHandler.cs - Adaptation triggering functionality
// - Handlers/LearningHandler.cs - Learning insights functionality
// - Handlers/EffectivenessHandler.cs - Effectiveness analysis functionality
// - Handlers/DashboardHandler.cs - Dashboard display functionality
// - Handlers/EnvironmentHandler.cs - Environment status functionality
// - Handlers/EngineHandler.cs - Engine control functionality
// - Utilities/IconHelper.cs - Icon display functionality

// Re-export all classes for backward compatibility
using Nexo.CLI.Commands.Adaptation.Core;
using Nexo.CLI.Commands.Adaptation.Commands;
using Nexo.CLI.Commands.Adaptation.Handlers;
using Nexo.CLI.Commands.Adaptation.Utilities;

namespace Nexo.CLI.Commands
{
    // All adaptation command classes are now available through the specialized files above
    // This maintains backward compatibility while providing focused, maintainable classes
}
