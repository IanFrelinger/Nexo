using Nexo.Core.Domain.Entities.FeatureFactory;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.FeatureFactory.FrameworkIntegration
{
    /// <summary>
    /// Framework-specific model classes
    /// </summary>
    public partial interface IFrameworkAdapter
    {
        // Framework-specific model classes are defined here
    }

    public partial class FrameworkFile
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public FileType Type { get; set; } = FileType.Code;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public partial class FrameworkDependency
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DependencyType Type { get; set; } = DependencyType.NuGet;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public partial class FrameworkConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Settings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Web API specific models
    public partial class WebApiController
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public partial class WebApiModel
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public partial class WebApiService
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public partial class WebApiConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Settings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Blazor specific models
    public partial class BlazorComponent
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public partial class BlazorPage
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public partial class BlazorService
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public partial class BlazorConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Settings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // MAUI specific models
    public partial class MauiPage
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public partial class MauiView
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public partial class MauiService
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public partial class MauiConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Settings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Console specific models
    public partial class ConsoleCommand
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public partial class ConsoleService
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public partial class ConsoleConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Settings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // WPF specific models
    public partial class WpfWindow
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public partial class WpfUserControl
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public partial class WpfViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public partial class WpfConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Settings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // WinForms specific models
    public partial class WinFormsForm
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public partial class WinFormsControl
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public partial class WinFormsService
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public partial class WinFormsConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Settings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Xamarin specific models
    public partial class XamarinPage
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public partial class XamarinView
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public partial class XamarinService
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public partial class XamarinConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Settings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
