using System.ComponentModel.DataAnnotations;

namespace FeatureFactoryDemo.Models
{
    /// <summary>
    /// Represents a successful command execution in the Feature Factory
    /// </summary>
    public class CommandHistory
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string Platform { get; set; } = string.Empty;
        
        [Required]
        public string GeneratedCode { get; set; } = string.Empty;
        
        [Required]
        public int FinalQualityScore { get; set; }
        
        [Required]
        public int IterationCount { get; set; }
        
        [Required]
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
        
        [MaxLength(1000)]
        public string? Context { get; set; }
        
        [MaxLength(200)]
        public string? Tags { get; set; }
        
        public bool IsSuccessful { get; set; } = true;
        
        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }
    }
    
    /// <summary>
    /// Represents codebase context information used for code generation
    /// </summary>
    public class CodebaseContext
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string FilePath { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string FileType { get; set; } = string.Empty;
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Summary { get; set; }
        
        [MaxLength(200)]
        public string? Patterns { get; set; }
        
        [Required]
        public DateTime LastAnalyzed { get; set; } = DateTime.UtcNow;
        
        [Required]
        public int QualityScore { get; set; }
        
        [MaxLength(1000)]
        public string? Violations { get; set; }
    }
    
    /// <summary>
    /// Represents a code generation pattern learned from successful commands
    /// </summary>
    public class CodePattern
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string PatternName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string PatternType { get; set; } = string.Empty;
        
        [Required]
        public string Template { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        [Required]
        public int UsageCount { get; set; } = 1;
        
        [Required]
        public DateTime FirstUsed { get; set; } = DateTime.UtcNow;
        
        [Required]
        public DateTime LastUsed { get; set; } = DateTime.UtcNow;
        
        [Required]
        public double SuccessRate { get; set; } = 1.0;
        
        [MaxLength(200)]
        public string? Tags { get; set; }
    }
}
