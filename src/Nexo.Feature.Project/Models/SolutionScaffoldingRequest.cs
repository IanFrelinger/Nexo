using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Core.Application.Enums;
using Nexo.Shared.Models;

namespace Nexo.Core.Application.Models
{
    /// <summary>
    /// Represents a request to scaffold a new solution.
    /// </summary>
    public sealed class SolutionScaffoldingRequest
    {
        public string SolutionName { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public SolutionScaffoldingConfiguration Configuration { get; set; } = new SolutionScaffoldingConfiguration();
        public bool Force { get; set; }
        public Dictionary<string, object> TemplateParameters { get; set; } = new Dictionary<string, object>();
        public string TargetFramework { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public bool CreateGitRepository { get; set; }
        public bool InitializeSolution { get; set; }
        public BuildConfiguration BuildConfiguration { get; set; } = new BuildConfiguration();

        public SolutionScaffoldingRequest()
        {
            SolutionName = string.Empty;
            OutputPath = string.Empty;
            TemplateName = string.Empty;
            Configuration = new SolutionScaffoldingConfiguration();
            Force = false;
            TemplateParameters = new Dictionary<string, object>();
            TargetFramework = "net8.0";
            Language = "C#";
            CreateGitRepository = true;
            InitializeSolution = true;
            BuildConfiguration = new BuildConfiguration
            {
                Configuration = "Debug",
                Platform = "Any CPU",
                RestorePackages = true,
                RunCodeAnalysis = true,
                TreatWarningsAsErrors = false
            };
        }
    }

    public sealed class SolutionScaffoldingConfiguration
    {
        public List<ProjectConfiguration> Projects { get; set; } = new List<ProjectConfiguration>();
        public SolutionStructureConfiguration Structure { get; set; } = new SolutionStructureConfiguration();
        public TestingConfiguration Testing { get; set; } = new TestingConfiguration();
        public CiCdConfiguration? CiCd { get; set; }
        public DocumentationConfiguration Documentation { get; set; } = new DocumentationConfiguration();
        public SecurityConfiguration Security { get; set; } = new SecurityConfiguration();
        public PerformanceConfiguration Performance { get; set; } = new PerformanceConfiguration();

        public SolutionScaffoldingConfiguration()
        {
            Projects = new List<ProjectConfiguration>();
            Structure = new SolutionStructureConfiguration();
            Testing = new TestingConfiguration();
            CiCd = null;
            Documentation = new DocumentationConfiguration();
            Security = new SecurityConfiguration();
            Performance = new PerformanceConfiguration();
        }
    }

    public sealed class ProjectConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public ProjectType Type { get; set; }
        public string TargetFramework { get; set; } = string.Empty;
        public string Template { get; set; } = string.Empty;
        public List<ProjectDependency> Dependencies { get; set; } = new List<ProjectDependency>();
        public List<NuGetPackage> NuGetPackages { get; set; } = new List<NuGetPackage>();
        public List<string> Folders { get; set; } = new List<string>();
        public List<FileConfiguration> Files { get; set; } = new List<FileConfiguration>();
        public bool IncludeTests { get; set; }
        public TestFramework TestFramework { get; set; }

        public ProjectConfiguration()
        {
            Name = string.Empty;
            Type = ProjectType.Console;
            TargetFramework = "net8.0";
            Template = string.Empty;
            Dependencies = new List<ProjectDependency>();
            NuGetPackages = new List<NuGetPackage>();
            Folders = new List<string>();
            Files = new List<FileConfiguration>();
            IncludeTests = true;
            TestFramework = TestFramework.xUnit;
        }
    }

    public sealed class SolutionStructureConfiguration
    {
        public FolderStructure FolderStructure { get; set; }
        public Dictionary<string, string> CustomFolderMappings { get; set; } = new Dictionary<string, string>();
        public bool UseSolutionFolders { get; set; }
        public List<SolutionFolderConfiguration> SolutionFolders { get; set; } = new List<SolutionFolderConfiguration>();

        public SolutionStructureConfiguration()
        {
            FolderStructure = FolderStructure.Standard;
            CustomFolderMappings = new Dictionary<string, string>();
            UseSolutionFolders = true;
            SolutionFolders = new List<SolutionFolderConfiguration>();
        }
    }

    public sealed class SolutionFolderConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Projects { get; set; } = new List<string>();
        public List<SolutionFolderConfiguration> Subfolders { get; set; } = new List<SolutionFolderConfiguration>();

        public SolutionFolderConfiguration()
        {
            Name = string.Empty;
            Projects = new List<string>();
            Subfolders = new List<SolutionFolderConfiguration>();
        }
    }

    public sealed class ProjectDependency
    {
        public string ProjectName { get; set; } = string.Empty;
        public ProjectDependencyType Type { get; set; }
        public bool IsRequired { get; set; }

        public ProjectDependency()
        {
            ProjectName = string.Empty;
            Type = ProjectDependencyType.Reference;
            IsRequired = true;
        }
    }

    public sealed class NuGetPackage
    {
        public string PackageId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public PackageType Type { get; set; }
        public bool IncludePrerelease { get; set; }

        public NuGetPackage()
        {
            PackageId = string.Empty;
            Version = "*";
            Type = PackageType.Reference;
            IncludePrerelease = false;
        }
    }

    public sealed class FileConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public string Template { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public bool Overwrite { get; set; }

        public FileConfiguration()
        {
            Name = string.Empty;
            Template = string.Empty;
            Path = string.Empty;
            Parameters = new Dictionary<string, object>();
            Overwrite = false;
        }
    }

    public sealed class TestingConfiguration
    {
        public TestFramework Framework { get; set; }
        public TestCoverageTool CoverageTool { get; set; }
        public int MinimumCoverage { get; set; }
        public bool IncludeIntegrationTests { get; set; }
        public bool IncludePerformanceTests { get; set; }

        public TestingConfiguration()
        {
            Framework = TestFramework.xUnit;
            CoverageTool = TestCoverageTool.Coverlet;
            MinimumCoverage = 80;
            IncludeIntegrationTests = true;
            IncludePerformanceTests = false;
        }
    }

    public sealed class CiCdConfiguration
    {
        public CiCdPlatform Platform { get; set; }
        public List<BuildTrigger> Triggers { get; set; } = new List<BuildTrigger>();
        public List<string> Environments { get; set; } = new List<string>();
        public bool EnableAutomatedTesting { get; set; }
        public bool EnableAutomatedDeployment { get; set; }

        public CiCdConfiguration()
        {
            Platform = CiCdPlatform.GitHubActions;
            Triggers = new List<BuildTrigger> { BuildTrigger.Push, BuildTrigger.PullRequest };
            Environments = new List<string> { "Development", "Staging", "Production" };
            EnableAutomatedTesting = true;
            EnableAutomatedDeployment = false;
        }
    }

    public sealed class DocumentationConfiguration
    {
        public DocumentationFormat Format { get; set; }
        public bool GenerateApiDocumentation { get; set; }
        public bool IncludeCodeExamples { get; set; }
        public List<string> Templates { get; set; } = new List<string>();

        public DocumentationConfiguration()
        {
            Format = DocumentationFormat.Markdown;
            GenerateApiDocumentation = true;
            IncludeCodeExamples = true;
            Templates = new List<string> { "README", "CONTRIBUTING", "CHANGELOG" };
        }
    }

    public sealed class SecurityConfiguration
    {
        public bool EnableSecurityScanning { get; set; }
        public List<SecurityTool> Tools { get; set; } = new List<SecurityTool>();
        public bool EnableSecretScanning { get; set; }
        public bool EnableDependencyVulnerabilityScanning { get; set; }

        public SecurityConfiguration()
        {
            EnableSecurityScanning = true;
            Tools = new List<SecurityTool> { SecurityTool.Snyk, SecurityTool.OWASPDependencyCheck };
            EnableSecretScanning = true;
            EnableDependencyVulnerabilityScanning = true;
        }
    }

    public sealed class PerformanceConfiguration
    {
        public bool EnablePerformanceMonitoring { get; set; }
        public List<PerformanceTool> Tools { get; set; } = new List<PerformanceTool>();
        public bool EnableProfiling { get; set; }
        public Dictionary<string, double> Thresholds { get; set; } = new Dictionary<string, double>();

        public PerformanceConfiguration()
        {
            EnablePerformanceMonitoring = true;
            Tools = new List<PerformanceTool> { PerformanceTool.ApplicationInsights };
            EnableProfiling = false;
            Thresholds = new Dictionary<string, double>();
        }
    }

    // Enums restored for C# 7.3 compatibility
    public enum ProjectType
    {
        Console,
        WebApi,
        Web,
        Library,
        Test,
        Blazor,
        Maui,
        Worker,
        Custom
    }

    public enum FolderStructure
    {
        Standard,
        CleanArchitecture,
        DomainDrivenDesign,
        Custom
    }

    public enum PackageType
    {
        Reference,
        Development,
        Tool
    }

    public enum TestFramework
    {
        xUnit,
        NUnit,
        MSTest
    }

    public enum TestCoverageTool
    {
        Coverlet,
        OpenCover,
        NCover
    }

    public enum BuildTrigger
    {
        Push,
        PullRequest,
        Tag,
        Manual
    }

    public enum CiCdPlatform
    {
        GitHubActions,
        AzureDevOps,
        GitLabCi,
        Jenkins,
        TeamCity
    }

    public enum DocumentationFormat
    {
        Markdown,
        Html,
        Pdf,
        Xml
    }

    public enum SecurityTool
    {
        Snyk,
        OWASPDependencyCheck,
        SonarQube,
        CodeQL
    }

    public enum PerformanceTool
    {
        ApplicationInsights,
        NewRelic,
        DataDog,
        Prometheus
    }
} 