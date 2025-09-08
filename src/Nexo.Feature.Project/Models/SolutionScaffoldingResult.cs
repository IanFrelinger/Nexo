using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexo.Core.Application.Enums;

namespace Nexo.Core.Application.Models
{
    public sealed class SolutionScaffoldingResult
    {
        public string ScaffoldingId { get; set; } = string.Empty;
        public string SolutionName { get; set; } = string.Empty;
        public string SolutionPath { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public ScaffoldingStatus Status { get; set; }
        public List<ScaffoldedProject> Projects { get; set; } = new List<ScaffoldedProject>();
        public List<GeneratedFile> GeneratedFiles { get; set; } = new List<GeneratedFile>();
        public DateTime ScaffoldingStartTime { get; set; }
        public DateTime ScaffoldingEndTime { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public SolutionBuildResult? BuildResult { get; set; }

        public SolutionScaffoldingResult()
        {
            ScaffoldingId = string.Empty;
            SolutionName = string.Empty;
            SolutionPath = string.Empty;
            TemplateName = string.Empty;
            Status = ScaffoldingStatus.Pending;
            Projects = new List<ScaffoldedProject>();
            GeneratedFiles = new List<GeneratedFile>();
            ScaffoldingStartTime = DateTime.MinValue;
            ScaffoldingEndTime = DateTime.MinValue;
            Errors = new List<string>();
            Warnings = new List<string>();
            BuildResult = null;
        }

        public TimeSpan ScaffoldingDuration { get { return ScaffoldingEndTime - ScaffoldingStartTime; } }
        public bool IsSuccessful { get { return Status == ScaffoldingStatus.Success && Errors.Count == 0; } }
        public string SolutionFilePath { get { return Path.Combine(SolutionPath, SolutionName + ".sln"); } }
        public int ProjectCount { get { return Projects.Count; } }
        public int FileCount { get { return GeneratedFiles.Count; } }
    }

    public sealed class ScaffoldedProject
    {
        public string Name { get; set; } = string.Empty;
        public ProjectType Type { get; set; }
        public string ProjectFilePath { get; set; } = string.Empty;
        public string TargetFramework { get; set; } = string.Empty;
        public List<string> Files { get; set; } = new List<string>();
        public List<NuGetPackage> NuGetPackages { get; set; } = new List<NuGetPackage>();
        public List<string> ProjectReferences { get; set; } = new List<string>();
        public bool IsCreated { get; set; }
        public List<string> Errors { get; set; } = new List<string>();

        public ScaffoldedProject()
        {
            Name = string.Empty;
            Type = ProjectType.Console;
            ProjectFilePath = string.Empty;
            TargetFramework = string.Empty;
            Files = new List<string>();
            NuGetPackages = new List<NuGetPackage>();
            ProjectReferences = new List<string>();
            IsCreated = true;
            Errors = new List<string>();
        }
    }

    public sealed class GeneratedFile
    {
        public string Name { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Template { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public bool IsGenerated { get; set; }
        public List<string> Errors { get; set; } = new List<string>();

        public GeneratedFile()
        {
            Name = string.Empty;
            FilePath = string.Empty;
            Template = string.Empty;
            FileSize = 0;
            IsGenerated = true;
            Errors = new List<string>();
        }

        public string FileExtension { get { return Path.GetExtension(FilePath); } }
    }

    public sealed class SolutionBuildResult
    {
        public string BuildId { get; set; } = string.Empty;
        public string SolutionPath { get; set; } = string.Empty;
        public string BuildConfiguration { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public BuildStatus Status { get; set; }
        public List<ProjectBuildResult> ProjectResults { get; set; } = new List<ProjectBuildResult>();
        public List<GeneratedDll> GeneratedDlls { get; set; } = new List<GeneratedDll>();
        public DateTime BuildStartTime { get; set; }
        public DateTime BuildEndTime { get; set; }
        public List<BuildError> Errors { get; set; } = new List<BuildError>();
        public List<BuildWarning> Warnings { get; set; } = new List<BuildWarning>();
        public string OutputDirectory { get; set; } = string.Empty;

        public SolutionBuildResult()
        {
            BuildId = string.Empty;
            SolutionPath = string.Empty;
            BuildConfiguration = string.Empty;
            Platform = string.Empty;
            Status = BuildStatus.Pending;
            ProjectResults = new List<ProjectBuildResult>();
            GeneratedDlls = new List<GeneratedDll>();
            BuildStartTime = DateTime.MinValue;
            BuildEndTime = DateTime.MinValue;
            Errors = new List<BuildError>();
            Warnings = new List<BuildWarning>();
            OutputDirectory = string.Empty;
        }

        public TimeSpan BuildDuration { get { return BuildEndTime - BuildStartTime; } }
        public bool IsSuccessful { get { return Status == BuildStatus.Success && Errors.Count == 0; } }
        public int SuccessfulProjectCount { get { return ProjectResults.Count(p => p.IsSuccessful); } }
        public int DllCount { get { return GeneratedDlls.Count; } }
    }

    public sealed class ProjectBuildResult
    {
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectFilePath { get; set; } = string.Empty;
        public BuildStatus Status { get; set; }
        public string OutputAssemblyPath { get; set; } = string.Empty;
        public string OutputDirectory { get; set; } = string.Empty;
        public List<BuildError> Errors { get; set; } = new List<BuildError>();
        public List<BuildWarning> Warnings { get; set; } = new List<BuildWarning>();
        public bool IsSuccessful { get { return Status == BuildStatus.Success && Errors.Count == 0; } }

        public ProjectBuildResult()
        {
            ProjectName = string.Empty;
            ProjectFilePath = string.Empty;
            Status = BuildStatus.Pending;
            OutputAssemblyPath = string.Empty;
            OutputDirectory = string.Empty;
            Errors = new List<BuildError>();
            Warnings = new List<BuildWarning>();
        }
    }

    public sealed class GeneratedDll
    {
        public string Name { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string BuildConfiguration { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string AssemblyVersion { get; set; } = string.Empty;
        public string FileVersion { get; set; } = string.Empty;
        public string ProductVersion { get; set; } = string.Empty;
        public bool IsGenerated { get; set; }

        public GeneratedDll()
        {
            Name = string.Empty;
            FilePath = string.Empty;
            FileSize = 0;
            ProjectName = string.Empty;
            BuildConfiguration = string.Empty;
            Platform = string.Empty;
            AssemblyVersion = string.Empty;
            FileVersion = string.Empty;
            ProductVersion = string.Empty;
            IsGenerated = true;
        }
    }

    public sealed class SolutionBuildRequest
    {
        public string SolutionPath { get; set; } = string.Empty;
        public string BuildConfiguration { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public bool Clean { get; set; }
        public bool Restore { get; set; }
        public bool Incremental { get; set; }
        public string OutputDirectory { get; set; } = string.Empty;
        public BuildVerbosity Verbosity { get; set; }
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        public SolutionBuildRequest()
        {
            SolutionPath = string.Empty;
            BuildConfiguration = "Release";
            Platform = "Any CPU";
            Clean = false;
            Restore = true;
            Incremental = true;
            OutputDirectory = string.Empty;
            Verbosity = BuildVerbosity.Normal;
            Properties = new Dictionary<string, string>();
        }
    }

    public sealed class ProjectScaffoldingRequest
    {
        public string ProjectName { get; set; } = string.Empty;
        public string SolutionPath { get; set; } = string.Empty;
        public ProjectType ProjectType { get; set; }
        public string TargetFramework { get; set; } = string.Empty;
        public string Template { get; set; } = string.Empty;
        public bool AddToSolution { get; set; }
        public string SolutionFolder { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        public ProjectScaffoldingRequest()
        {
            ProjectName = string.Empty;
            SolutionPath = string.Empty;
            ProjectType = ProjectType.Console;
            TargetFramework = "net8.0";
            Template = string.Empty;
            AddToSolution = true;
            SolutionFolder = string.Empty;
            Parameters = new Dictionary<string, object>();
        }
    }

    public sealed class ProjectScaffoldingResult
    {
        public string ScaffoldingId { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectFilePath { get; set; } = string.Empty;
        public ProjectType ProjectType { get; set; }
        public ScaffoldingStatus Status { get; set; }
        public List<GeneratedFile> GeneratedFiles { get; set; } = new List<GeneratedFile>();
        public bool AddedToSolution { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public bool IsSuccessful { get { return Status == ScaffoldingStatus.Success && Errors.Count == 0; } }

        public ProjectScaffoldingResult()
        {
            ScaffoldingId = string.Empty;
            ProjectName = string.Empty;
            ProjectFilePath = string.Empty;
            ProjectType = ProjectType.Console;
            Status = ScaffoldingStatus.Pending;
            GeneratedFiles = new List<GeneratedFile>();
            AddedToSolution = false;
            Errors = new List<string>();
        }
    }

    public sealed class CodeGenerationRequest
    {
        public string Template { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public bool Overwrite { get; set; }
        public string Encoding { get; set; } = string.Empty;

        public CodeGenerationRequest()
        {
            Template = string.Empty;
            OutputPath = string.Empty;
            Parameters = new Dictionary<string, object>();
            Overwrite = false;
            Encoding = "UTF-8";
        }
    }

    public sealed class CodeGenerationResult
    {
        public string GenerationId { get; set; } = string.Empty;
        public string Template { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public ScaffoldingStatus Status { get; set; }
        public string GeneratedContent { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public bool IsSuccessful { get { return Status == ScaffoldingStatus.Success && Errors.Count == 0; } }

        public CodeGenerationResult()
        {
            GenerationId = string.Empty;
            Template = string.Empty;
            OutputPath = string.Empty;
            Status = ScaffoldingStatus.Pending;
            GeneratedContent = string.Empty;
            FileSize = 0;
            Errors = new List<string>();
        }
    }

    public sealed class ScaffoldingValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Suggestions { get; set; } = new List<string>();

        public ScaffoldingValidationResult()
        {
            IsValid = false;
            Errors = new List<string>();
            Warnings = new List<string>();
            Suggestions = new List<string>();
        }
    }

    public sealed class SolutionTemplate
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
        public string TemplatePath { get; set; } = string.Empty;
        public List<TemplateParameter> Parameters { get; set; } = new List<TemplateParameter>();
        public bool IsBuiltIn { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }

        public SolutionTemplate()
        {
            Name = string.Empty;
            Description = string.Empty;
            Author = string.Empty;
            Version = "1.0.0";
            Tags = new List<string>();
            TemplatePath = string.Empty;
            Parameters = new List<TemplateParameter>();
            IsBuiltIn = false;
            CreatedAt = DateTime.UtcNow;
            LastModified = DateTime.UtcNow;
        }
    }

    public sealed class TemplateParameter
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ParameterType Type { get; set; }
        public object? DefaultValue { get; set; }
        public bool IsRequired { get; set; }
        public List<object> AllowedValues { get; set; } = new List<object>();

        public TemplateParameter()
        {
            Name = string.Empty;
            Description = string.Empty;
            Type = ParameterType.String;
            DefaultValue = null;
            IsRequired = false;
            AllowedValues = new List<object>();
        }
    }

    // Enums restored for C# 7.3 compatibility
    public enum ScaffoldingStatus
    {
        Pending,
        InProgress,
        Success,
        SuccessWithWarnings,
        Failed,
        Cancelled
    }

    public enum BuildStatus
    {
        Pending,
        InProgress,
        Success,
        SuccessWithWarnings,
        Failed,
        Cancelled
    }

    public enum BuildVerbosity
    {
        Quiet,
        Minimal,
        Normal,
        Detailed,
        Diagnostic
    }

    public enum ParameterType
    {
        String,
        Integer,
        Boolean,
        Enum,
        List
    }
} 