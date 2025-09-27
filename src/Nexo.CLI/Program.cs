// This file has been refactored into specialized CLI program classes:
// - Core/Program.cs - Main entry point and host configuration
// - Commands/CommandOrchestrator.cs - Command orchestration and setup
// - Commands/AICommandHandler.cs - AI-related command handling
// - Commands/PipelineCommandHandler.cs - Pipeline-related command handling
// - Configuration/StubPipelineConfiguration.cs - Stub configuration implementation

// Re-export all classes for backward compatibility
using Nexo.CLI.Program.Core;
using Nexo.CLI.Program.Commands;
using Nexo.CLI.Program.Configuration;

namespace Nexo.CLI
{
    // All CLI program classes are now available through the specialized files above
    // This maintains backward compatibility while providing focused, maintainable classes
}
