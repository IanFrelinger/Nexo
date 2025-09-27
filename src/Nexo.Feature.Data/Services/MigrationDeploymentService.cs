// This file has been refactored into specialized migration deployment classes:
// - Core/MigrationDeploymentService.cs - Main orchestrator class
// - Testing/MigrationTestRunner.cs - Pre and post-deployment testing
// - Backup/DatabaseBackupService.cs - Database backup operations
// - Execution/MigrationExecutionOrchestrator.cs - Migration execution orchestration
// - Models/MigrationDeploymentModels.cs - All deployment models and results

// Re-export all classes for backward compatibility
using Nexo.Feature.Data.Services.MigrationDeployment.Core;
using Nexo.Feature.Data.Services.MigrationDeployment.Testing;
using Nexo.Feature.Data.Services.MigrationDeployment.Backup;
using Nexo.Feature.Data.Services.MigrationDeployment.Execution;
using Nexo.Feature.Data.Services.MigrationDeployment.Models;

namespace Nexo.Feature.Data.Services
{
    // All migration deployment classes are now available through the specialized files above
    // This maintains backward compatibility while providing focused, maintainable classes
}
