using System;

namespace Nexo.Core.Domain.Models.Export
{
    /// <summary>
    /// Base class for export options
    /// </summary>
    public abstract record ExportOptions
    {
        /// <summary>
        /// Output directory for the export
        /// </summary>
        public string OutputDirectory { get; init; } = string.Empty;
        
        /// <summary>
        /// Whether to overwrite existing files
        /// </summary>
        public bool Overwrite { get; init; } = false;
        
        /// <summary>
        /// Whether to include metadata
        /// </summary>
        public bool IncludeMetadata { get; init; } = true;
        
        /// <summary>
        /// Whether to include CLI export
        /// </summary>
        public bool IncludeCli { get; init; } = false;
        
        /// <summary>
        /// Whether to include Docker export
        /// </summary>
        public bool IncludeDocker { get; init; } = false;
        
        /// <summary>
        /// Whether to include package export
        /// </summary>
        public bool IncludePackage { get; init; } = false;
        
        /// <summary>
        /// Whether to include documentation export
        /// </summary>
        public bool IncludeDocumentation { get; init; } = false;
        
        /// <summary>
        /// CLI export options
        /// </summary>
        public CliExportOptions CliOptions { get; init; } = new();
        
        /// <summary>
        /// Docker export options
        /// </summary>
        public DockerExportOptions DockerOptions { get; init; } = new();
        
        /// <summary>
        /// Package export options
        /// </summary>
        public PackageExportOptions PackageOptions { get; init; } = new();
        
        /// <summary>
        /// Documentation export options
        /// </summary>
        public DocumentationExportOptions DocumentationOptions { get; init; } = new();
    }
    
    /// <summary>
    /// Options for CLI export
    /// </summary>
    public record CliExportOptions : ExportOptions
    {
        /// <summary>
        /// Whether to include executable files
        /// </summary>
        public bool IncludeExecutables { get; init; } = true;
        
        /// <summary>
        /// Whether to include configuration files
        /// </summary>
        public bool IncludeConfig { get; init; } = true;
        
        /// <summary>
        /// Platform for CLI export
        /// </summary>
        public string Platform { get; init; } = "dotnet";
        
        /// <summary>
        /// Whether to enable AI features
        /// </summary>
        public bool EnableAI { get; init; } = false;
        
        /// <summary>
        /// Whether to enable platform features
        /// </summary>
        public bool EnablePlatform { get; init; } = false;
        
        /// <summary>
        /// Whether to enable analysis features
        /// </summary>
        public bool EnableAnalysis { get; init; } = false;
    }
    
    /// <summary>
    /// Options for Docker export
    /// </summary>
    public record DockerExportOptions : ExportOptions
    {
        /// <summary>
        /// Base image to use
        /// </summary>
        public string BaseImage { get; init; } = "mcr.microsoft.com/dotnet/aspnet:8.0";
        
        /// <summary>
        /// Whether to include source code
        /// </summary>
        public bool IncludeSource { get; init; } = false;
        
        /// <summary>
        /// Port to expose
        /// </summary>
        public int Port { get; init; } = 8080;
        
        /// <summary>
        /// Environment for Docker export
        /// </summary>
        public string Environment { get; init; } = "production";
    }
    
    /// <summary>
    /// Options for package export
    /// </summary>
    public record PackageExportOptions : ExportOptions
    {
        /// <summary>
        /// Package format (zip, tar, etc.)
        /// </summary>
        public string Format { get; init; } = "zip";
        
        /// <summary>
        /// Whether to compress the package
        /// </summary>
        public bool Compress { get; init; } = true;
        
        /// <summary>
        /// Package version
        /// </summary>
        public string Version { get; init; } = "1.0.0";
        
        /// <summary>
        /// Platform for package export
        /// </summary>
        public string Platform { get; init; } = "dotnet";
        
        /// <summary>
        /// Whether to enable auto-update
        /// </summary>
        public bool EnableAutoUpdate { get; init; } = false;
        
        /// <summary>
        /// Whether to enable telemetry
        /// </summary>
        public bool EnableTelemetry { get; init; } = false;
        
        /// <summary>
        /// Whether to enable analytics
        /// </summary>
        public bool EnableAnalytics { get; init; } = false;
    }
    
    /// <summary>
    /// Options for documentation export
    /// </summary>
    public record DocumentationExportOptions : ExportOptions
    {
        /// <summary>
        /// Documentation format (markdown, html, etc.)
        /// </summary>
        public string Format { get; init; } = "markdown";
        
        /// <summary>
        /// Whether to include code examples
        /// </summary>
        public bool IncludeCodeExamples { get; init; } = true;
        
        /// <summary>
        /// Whether to include API documentation
        /// </summary>
        public bool IncludeApiDocs { get; init; } = true;
        
        /// <summary>
        /// Whether to include user guides
        /// </summary>
        public bool IncludeUserGuides { get; init; } = true;
        
        /// <summary>
        /// Whether to include developer guides
        /// </summary>
        public bool IncludeDeveloperGuides { get; init; } = true;
        
        /// <summary>
        /// Whether to include examples
        /// </summary>
        public bool IncludeExamples { get; init; } = true;
        
        /// <summary>
        /// Documentation theme
        /// </summary>
        public string Theme { get; init; } = "default";
    }
}
