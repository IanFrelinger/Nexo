using System.Collections.Generic;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Services
{
    /// <summary>
    /// Template initialization functionality for workflow configuration service.
    /// </summary>
    public partial class WorkflowConfigurationService
    {
        private void InitializeDefaultTemplates()
        {
            // Setup workflow template
            var setupTemplate = new WorkflowConfiguration
            {
                Name = "Default Setup Workflow",
                Description = "Default workflow for setting up a new development environment",
                Type = WorkflowType.Setup,
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep
                    {
                        Name = "Check Prerequisites",
                        Description = "Check if required tools and dependencies are installed",
                        Type = StepType.Command,
                        Command = "dotnet",
                        Arguments = new List<string> { "--version" },
                        IsRequired = true
                    },
                    new WorkflowStep
                    {
                        Name = "Restore Dependencies",
                        Description = "Restore NuGet packages and project dependencies",
                        Type = StepType.Command,
                        Command = "dotnet",
                        Arguments = new List<string> { "restore" },
                        IsRequired = true
                    },
                    new WorkflowStep
                    {
                        Name = "Build Project",
                        Description = "Build the project to ensure everything compiles",
                        Type = StepType.Command,
                        Command = "dotnet",
                        Arguments = new List<string> { "build" },
                        IsRequired = true
                    }
                }
            };

            // Analyze workflow template
            var analyzeTemplate = new WorkflowConfiguration
            {
                Name = "Default Analysis Workflow",
                Description = "Default workflow for code analysis and quality checks",
                Type = WorkflowType.Analyze,
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep
                    {
                        Name = "Code Analysis",
                        Description = "Run static code analysis",
                        Type = StepType.Command,
                        Command = "dotnet",
                        Arguments = new List<string> { "analyze" },
                        IsRequired = true
                    },
                    new WorkflowStep
                    {
                        Name = "Style Check",
                        Description = "Check code style and formatting",
                        Type = StepType.Command,
                        Command = "dotnet",
                        Arguments = new List<string> { "format", "--verify-no-changes" },
                        IsRequired = false
                    }
                }
            };

            // Test workflow template
            var testTemplate = new WorkflowConfiguration
            {
                Name = "Default Test Workflow",
                Description = "Default workflow for running tests and generating reports",
                Type = WorkflowType.Test,
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep
                    {
                        Name = "Run Tests",
                        Description = "Run all unit tests",
                        Type = StepType.Command,
                        Command = "dotnet",
                        Arguments = new List<string> { "test" },
                        IsRequired = true
                    },
                    new WorkflowStep
                    {
                        Name = "Generate Coverage Report",
                        Description = "Generate code coverage report",
                        Type = StepType.Command,
                        Command = "dotnet",
                        Arguments = new List<string> { "test", "--collect", "XPlat Code Coverage" },
                        IsRequired = false
                    }
                }
            };

            // Deploy workflow template
            var deployTemplate = new WorkflowConfiguration
            {
                Name = "Default Deploy Workflow",
                Description = "Default workflow for building and deploying applications",
                Type = WorkflowType.Deploy,
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep
                    {
                        Name = "Build Release",
                        Description = "Build the project in release configuration",
                        Type = StepType.Command,
                        Command = "dotnet",
                        Arguments = new List<string> { "build", "--configuration", "Release" },
                        IsRequired = true
                    },
                    new WorkflowStep
                    {
                        Name = "Run Tests",
                        Description = "Run tests before deployment",
                        Type = StepType.Command,
                        Command = "dotnet",
                        Arguments = new List<string> { "test" },
                        IsRequired = true
                    },
                    new WorkflowStep
                    {
                        Name = "Publish Application",
                        Description = "Publish the application for deployment",
                        Type = StepType.Command,
                        Command = "dotnet",
                        Arguments = new List<string> { "publish", "--configuration", "Release" },
                        IsRequired = true
                    }
                }
            };

            _templates["default-setup"] = setupTemplate;
            _templates["default-analyze"] = analyzeTemplate;
            _templates["default-test"] = testTemplate;
            _templates["default-deploy"] = deployTemplate;
        }
    }
}
