using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Core.Domain.Models.GuidedGeneration;

namespace Nexo.Infrastructure.GuidedGeneration
{
    /// <summary>
    /// Step definition and input handling functionality for GuidedGenerationService.
    /// </summary>
    public partial class GuidedGenerationService
    {
        /// <summary>
        /// Creates the generation steps for a new session.
        /// </summary>
        private List<GenerationStep> CreateGenerationSteps()
        {
            return new List<GenerationStep>
            {
                new GenerationStep
                {
                    Order = 1,
                    Title = "Tool Name",
                    Description = "What would you like to call your tool?",
                    Question = "Enter a name for your tool:",
                    Type = StepType.TextInput,
                    IsRequired = true,
                    ValidationRule = new ValidationRule
                    {
                        Type = ValidationType.Required,
                        MinLength = 3,
                        MaxLength = 50,
                        ErrorMessage = "Tool name must be between 3 and 50 characters"
                    },
                    Examples = new List<string> { "json-formatter", "api-tester", "csv-converter" }
                },
                new GenerationStep
                {
                    Order = 2,
                    Title = "Tool Category",
                    Description = "What type of tool is this?",
                    Question = "Select a category:",
                    Type = StepType.Choice,
                    IsRequired = true,
                    Options = new List<string>
                    {
                        "Data Processing",
                        "API Testing",
                        "File Conversion",
                        "Text Processing",
                        "Database Operations",
                        "System Utilities",
                        "Other"
                    },
                    ValidationRule = new ValidationRule
                    {
                        Type = ValidationType.Choice,
                        AllowedValues = new List<string>
                        {
                            "Data Processing",
                            "API Testing",
                            "File Conversion",
                            "Text Processing",
                            "Database Operations",
                            "System Utilities",
                            "Other"
                        }
                    }
                },
                new GenerationStep
                {
                    Order = 3,
                    Title = "Tool Description",
                    Description = "What should this tool do?",
                    Question = "Describe what your tool should do:",
                    Type = StepType.TextArea,
                    IsRequired = true,
                    ValidationRule = new ValidationRule
                    {
                        Type = ValidationType.Required,
                        MinLength = 10,
                        MaxLength = 500,
                        ErrorMessage = "Description must be between 10 and 500 characters"
                    },
                    Examples = new List<string>
                    {
                        "Convert CSV files to JSON format with customizable field mapping",
                        "Test REST API endpoints and generate detailed reports",
                        "Format and validate JSON data with pretty printing"
                    }
                },
                new GenerationStep
                {
                    Order = 4,
                    Title = "Inputs",
                    Description = "What inputs does your tool need?",
                    Question = "List the inputs your tool requires (one per line, or press Enter to skip):",
                    Type = StepType.TextArea,
                    IsRequired = false,
                    HelpText = "Examples: file path, API URL, configuration options, etc."
                },
                new GenerationStep
                {
                    Order = 5,
                    Title = "Outputs",
                    Description = "What should your tool produce?",
                    Question = "List the outputs your tool should generate (one per line, or press Enter to skip):",
                    Type = StepType.TextArea,
                    IsRequired = false,
                    HelpText = "Examples: formatted file, test report, processed data, etc."
                },
                new GenerationStep
                {
                    Order = 6,
                    Title = "Requirements",
                    Description = "Any specific requirements or constraints?",
                    Question = "List any specific requirements (one per line, or press Enter to skip):",
                    Type = StepType.TextArea,
                    IsRequired = false,
                    HelpText = "Examples: must work with large files, needs error handling, requires specific libraries, etc."
                },
                new GenerationStep
                {
                    Order = 7,
                    Title = "Confirmation",
                    Description = "Review your tool specification",
                    Question = "Does this look correct? (y/n)",
                    Type = StepType.Confirmation,
                    IsRequired = true,
                    ValidationRule = new ValidationRule
                    {
                        Type = ValidationType.Choice,
                        AllowedValues = new List<string> { "y", "yes", "n", "no" }
                    }
                }
            };
        }

        /// <summary>
        /// Stores step input based on step type.
        /// </summary>
        private void StoreStepInput(GenerationSession session, GenerationStep step, string input)
        {
            switch (step.Order)
            {
                case 1: // Tool Name
                    session.ToolName = input;
                    break;
                case 2: // Category
                    session.Category = input;
                    break;
                case 3: // Description
                    session.Description = input;
                    break;
                case 4: // Inputs
                    session.Inputs = input.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                        .Select(i => i.Trim())
                                        .Where(i => !string.IsNullOrEmpty(i))
                                        .ToList();
                    break;
                case 5: // Outputs
                    session.Outputs = input.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                         .Select(o => o.Trim())
                                         .Where(o => !string.IsNullOrEmpty(o))
                                         .ToList();
                    break;
                case 6: // Requirements
                    session.Requirements = input.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                              .Select(r => r.Trim())
                                              .Where(r => !string.IsNullOrEmpty(r))
                                              .ToList();
                    break;
            }
        }
    }
}
