using System;
using System.Collections.Generic;
using Nexo.Core.Domain.Models.CodeQuality;

namespace Nexo.Core.Domain.Models.GuidedGeneration
{
    /// <summary>
    /// Represents a guided tool generation session
    /// </summary>
    public class GenerationSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public GenerationStatus Status { get; set; } = GenerationStatus.InProgress;
        
        // Session data
        public string ToolName { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public List<string> Inputs { get; set; } = new();
        public List<string> Outputs { get; set; } = new();
        public List<string> Requirements { get; set; } = new();
        public Dictionary<string, object> AdditionalContext { get; set; } = new();
        
        // Generation steps
        public List<GenerationStep> Steps { get; set; } = new();
        public int CurrentStepIndex { get; set; } = 0;
        
        // Results
        public string? GeneratedCode { get; set; }
        public QualityScore? QualityScore { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        
        // Progress tracking
        public double ProgressPercentage => Steps.Count > 0 ? (double)CurrentStepIndex / Steps.Count * 100 : 0;
        public bool IsComplete => Status == GenerationStatus.Completed || Status == GenerationStatus.Failed;
        public bool CanProceed => CurrentStepIndex < Steps.Count && Status == GenerationStatus.InProgress;
        
        /// <summary>
        /// Gets the current step
        /// </summary>
        public GenerationStep? CurrentStep => CanProceed ? Steps[CurrentStepIndex] : null;
        
        /// <summary>
        /// Moves to the next step
        /// </summary>
        public bool MoveToNextStep()
        {
            if (CanProceed)
            {
                CurrentStepIndex++;
                if (CurrentStepIndex >= Steps.Count)
                {
                    Status = GenerationStatus.Completed;
                    CompletedAt = DateTime.UtcNow;
                }
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Moves to the previous step
        /// </summary>
        public bool MoveToPreviousStep()
        {
            if (CurrentStepIndex > 0)
            {
                CurrentStepIndex--;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Marks the session as failed
        /// </summary>
        public void MarkAsFailed(string error)
        {
            Status = GenerationStatus.Failed;
            CompletedAt = DateTime.UtcNow;
            Errors.Add(error);
        }
    }

    /// <summary>
    /// Represents a single step in the guided generation process
    /// </summary>
    public class GenerationStep
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int Order { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Question { get; set; } = "";
        public StepType Type { get; set; }
        public bool IsRequired { get; set; } = true;
        public bool IsCompleted { get; set; } = false;
        public string? UserInput { get; set; }
        public List<string> Options { get; set; } = new();
        public List<string> Examples { get; set; } = new();
        public string? HelpText { get; set; }
        public ValidationRule? ValidationRule { get; set; }
    }

    /// <summary>
    /// Validation rule for step input
    /// </summary>
    public class ValidationRule
    {
        public ValidationType Type { get; set; }
        public string? Pattern { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public List<string> AllowedValues { get; set; } = new();
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Validates the input
        /// </summary>
        public ValidationResult Validate(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Input is required"
                };
            }
            
            switch (Type)
            {
                case ValidationType.Required:
                    return new ValidationResult
                    {
                        IsValid = !string.IsNullOrWhiteSpace(input),
                        ErrorMessage = string.IsNullOrWhiteSpace(input) ? "This field is required" : null
                    };
                    
                case ValidationType.Length:
                    var length = input.Length;
                    var isValidLength = (!MinLength.HasValue || length >= MinLength.Value) &&
                                      (!MaxLength.HasValue || length <= MaxLength.Value);
                    return new ValidationResult
                    {
                        IsValid = isValidLength,
                        ErrorMessage = isValidLength ? null : 
                            $"Length must be between {MinLength ?? 0} and {MaxLength ?? int.MaxValue} characters"
                    };
                    
                case ValidationType.Pattern:
                    var isValidPattern = string.IsNullOrEmpty(Pattern) || 
                                       System.Text.RegularExpressions.Regex.IsMatch(input, Pattern);
                    return new ValidationResult
                    {
                        IsValid = isValidPattern,
                        ErrorMessage = isValidPattern ? null : ErrorMessage ?? "Input format is invalid"
                    };
                    
                case ValidationType.Choice:
                    var isValidChoice = AllowedValues.Contains(input, StringComparer.OrdinalIgnoreCase);
                    return new ValidationResult
                    {
                        IsValid = isValidChoice,
                        ErrorMessage = isValidChoice ? null : 
                            $"Must be one of: {string.Join(", ", AllowedValues)}"
                    };
                    
                default:
                    return new ValidationResult { IsValid = true };
            }
        }
    }

    /// <summary>
    /// Validation result
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Generation status
    /// </summary>
    public enum GenerationStatus
    {
        InProgress,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Step types
    /// </summary>
    public enum StepType
    {
        TextInput,
        Choice,
        MultiChoice,
        TextArea,
        FileUpload,
        Confirmation
    }

    /// <summary>
    /// Validation types
    /// </summary>
    public enum ValidationType
    {
        Required,
        Length,
        Pattern,
        Choice,
        Email,
        Url
    }
}
