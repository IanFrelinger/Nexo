// This file has been refactored into specialized model files:
// - Requests/PipelineRequests.cs - Pipeline request models
// - Results/PipelineResults.cs - Pipeline result models
// - Status/PipelineStatus.cs - Status and health models
// - Analysis/AnalysisModels.cs - Analysis-related models
// - Platform/PlatformModels.cs - Platform-related models
// - Enums/PipelineEnums.cs - Pipeline enums
// - Validation/ValidationModels.cs - Validation and optimization models

// Re-export all models for backward compatibility
using Nexo.Feature.Pipeline.Models.Requests;
using Nexo.Feature.Pipeline.Models.Results;
using Nexo.Feature.Pipeline.Models.Status;
using Nexo.Feature.Pipeline.Models.Analysis;
using Nexo.Feature.Pipeline.Models.Platform;
using Nexo.Feature.Pipeline.Models.Enums;
using Nexo.Feature.Pipeline.Models.Validation;

namespace Nexo.Feature.Pipeline.Models
{
    // All models are now available through the specialized files above
    // This maintains backward compatibility while providing focused, maintainable model classes
} 