using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexo.Core.Application.Enums;

namespace Nexo.Core.Application.Models
{
    public sealed class SolutionScaffoldingResult
    {
        public string ScaffoldingId { get; set; }
        public string SolutionName { get; set; }
        public string SolutionPath { get; set; }
        public string TemplateName { get; set; }
        public ScaffoldingStatus Status { get; set; }
        public List<ScaffoldedProject> Projects { get; set; }
        public List<GeneratedFile> GeneratedFiles { get; set; }
        public DateTime ScaffoldingStartTime { get; set; }
        public DateTime ScaffoldingEndTime { get; set; }
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }
        public SolutionBuildResult BuildResult { get; set; }

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
        public string Name { get; set; }
        public ProjectType Type { get; set; }
        public string ProjectFilePath { get; set; }
        public string TargetFramework { get; set; }
        public List<string> Files { get; set; }
        public List<NuGetPackage> NuGetPackages { get; set; }
        public List<string> ProjectReferences { get; set; }
        public bool IsCreated { get; set; }
        public List<string> Errors { get; set; }

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
        public string Name { get; set; }
        public string FilePath { get; set; }
        public string Template { get; set; }
        public long FileSize { get; set; }
        public bool IsGenerated { get; set; }
        public List<string> Errors { get; set; }

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
        public string BuildId { get; set; }
        public string SolutionPath { get; set; }
        public string BuildConfiguration { get; set; }
        public string Platform { get; set; }
        public BuildStatus Status { get; set; }
        public List<ProjectBuildResult> ProjectResults { get; set; }
        public List<GeneratedDll> GeneratedDlls { get; set; }
        public DateTime BuildStartTime { get; set; }
        public DateTime BuildEndTime { get; set; }
        public List<BuildError> Errors { get; set; }
        public List<BuildWarning> Warnings { get; set; }
        public string OutputDirectory { get; set; }

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
        public string ProjectName { get; set; }
        public string ProjectFilePath { get; set; }
        public BuildStatus Status { get; set; }
        public string OutputAssemblyPath { get; set; }
        public string OutputDirectory { get; set; }
        public List<BuildError> Errors { get; set; }
        public List<BuildWarning> Warnings { get; set; }
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
        public string Name { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string ProjectName { get; set; }
        public string BuildConfiguration { get; set; }
        public string Platform { get; set; }
        public string AssemblyVersion { get; set; }
        public string FileVersion { get; set; }
        public string ProductVersion { get; set; }
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
        public string SolutionPath { get; set; }
        public string BuildConfiguration { get; set; }
        public string Platform { get; set; }
        public bool Clean { get; set; }
        public bool Restore { get; set; }
        public bool Incremental { get; set; }
        public string OutputDirectory { get; set; }
        public BuildVerbosity Verbosity { get; set; }
        public Dictionary<string, string> Properties { get; set; }

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
        public string ProjectName { get; set; }
        public string SolutionPath { get; set; }
        public ProjectType ProjectType { get; set; }
        public string TargetFramework { get; set; }
        public string Template { get; set; }
        public bool AddToSolution { get; set; }
        public string SolutionFolder { get; set; }
        public Dictionary<string, object> Parameters { get; set; }

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
        public string ScaffoldingId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectFilePath { get; set; }
        public ProjectType ProjectType { get; set; }
        public ScaffoldingStatus Status { get; set; }
        public List<GeneratedFile> GeneratedFiles { get; set; }
        public bool AddedToSolution { get; set; }
        public List<string> Errors { get; set; }
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
        public string Template { get; set; }
        public string OutputPath { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public bool Overwrite { get; set; }
        public string Encoding { get; set; }

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
        public string GenerationId { get; set; }
        public string Template { get; set; }
        public string OutputPath { get; set; }
        public ScaffoldingStatus Status { get; set; }
        public string GeneratedContent { get; set; }
        public long FileSize { get; set; }
        public List<string> Errors { get; set; }
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
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }
        public List<string> Suggestions { get; set; }

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
        public string Name { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public string Version { get; set; }
        public List<string> Tags { get; set; }
        public string TemplatePath { get; set; }
        public List<TemplateParameter> Parameters { get; set; }
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
        public string Name { get; set; }
        public string Description { get; set; }
        public ParameterType Type { get; set; }
        public object DefaultValue { get; set; }
        public bool IsRequired { get; set; }
        public List<object> AllowedValues { get; set; }

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