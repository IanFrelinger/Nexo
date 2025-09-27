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

    public class FrameworkFile
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public FileType Type { get; set; } = FileType.Code;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class FrameworkDependency
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DependencyType Type { get; set; } = DependencyType.NuGet;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class FrameworkConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Settings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Web API specific models
    public class WebApiController
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class WebApiModel
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class WebApiService
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class WebApiConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Settings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Blazor specific models
    public class BlazorComponent
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class BlazorPage
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class BlazorService
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class BlazorConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Settings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // MAUI specific models
    public class MauiPage
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class MauiView
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class MauiService
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class MauiConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Settings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Console specific models
    public class ConsoleCommand
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ConsoleService
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ConsoleConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Settings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // WPF specific models
    public class WpfWindow
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class WpfUserControl
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class WpfViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class WpfConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Settings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // WinForms specific models
    public class WinFormsForm
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class WinFormsControl
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class WinFormsService
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class WinFormsConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Settings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Xamarin specific models
    public class XamarinPage
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class XamarinView
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class XamarinService
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class XamarinConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Settings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
