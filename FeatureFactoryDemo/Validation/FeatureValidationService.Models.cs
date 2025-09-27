namespace FeatureFactoryDemo.Validation
{
    // Validation result classes
    public class ValidationResults
    {
        public DatabaseValidationResult DatabaseValidation { get; set; } = new();
        public CodebaseValidationResult CodebaseValidation { get; set; } = new();
        public CommandHistoryValidationResult CommandHistoryValidation { get; set; } = new();
        public IterativeImprovementValidationResult IterativeImprovementValidation { get; set; } = new();
        public IntegrationValidationResult IntegrationValidation { get; set; } = new();
    }
    
    public class DatabaseValidationResult
    {
        public bool IsValid { get; set; }
        public bool CanConnect { get; set; }
        public bool TablesExist { get; set; }
        public bool CanCreate { get; set; }
        public bool CanRead { get; set; }
        public bool CanUpdate { get; set; }
        public bool CanDelete { get; set; }
        public string? ErrorMessage { get; set; }
    }
    
    public class CodebaseValidationResult
    {
        public bool IsValid { get; set; }
        public int FilesAnalyzed { get; set; }
        public int AverageQuality { get; set; }
        public int HighQualityFiles { get; set; }
        public bool CanRetrieveContext { get; set; }
        public bool CodeAnalysisWorks { get; set; }
        public string? ErrorMessage { get; set; }
    }
    
    public class CommandHistoryValidationResult
    {
        public bool IsValid { get; set; }
        public bool CanSaveCommand { get; set; }
        public bool CanRetrieveRecent { get; set; }
        public bool CanFindSimilar { get; set; }
        public bool CanGetStatistics { get; set; }
        public string? ErrorMessage { get; set; }
    }
    
    public class IterativeImprovementValidationResult
    {
        public bool IsValid { get; set; }
        public bool CanAnalyzeCode { get; set; }
        public bool CanImproveCode { get; set; }
        public bool QualityImproved { get; set; }
        public bool CanSaveToDatabase { get; set; }
        public string? ErrorMessage { get; set; }
    }
    
    public class IntegrationValidationResult
    {
        public bool IsValid { get; set; }
        public bool CanGetContext { get; set; }
        public bool CanGetSimilarCommands { get; set; }
        public bool CanAnalyzeGeneratedCode { get; set; }
        public bool CanSaveGeneratedCode { get; set; }
        public bool StatisticsUpdated { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
