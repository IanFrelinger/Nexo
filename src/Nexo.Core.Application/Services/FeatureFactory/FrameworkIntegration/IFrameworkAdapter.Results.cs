using Nexo.Core.Domain.Entities.FeatureFactory;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.FeatureFactory.FrameworkIntegration
{
    /// <summary>
    /// Framework generation results
    /// </summary>
    public partial interface IFrameworkAdapter
    {
        // Framework generation results are defined here
    }

    /// <summary>
    /// Result of framework code generation
    /// </summary>
    public partial class FrameworkResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public FrameworkType Framework { get; set; }
        public List<FrameworkFile> Files { get; set; } = new();
        public List<FrameworkDependency> Dependencies { get; set; } = new();
        public FrameworkConfiguration Configuration { get; set; } = new();
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of Web API code generation
    /// </summary>
    public partial class WebApiResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<WebApiController> Controllers { get; set; } = new();
        public List<WebApiModel> Models { get; set; } = new();
        public List<WebApiService> Services { get; set; } = new();
        public WebApiConfiguration Configuration { get; set; } = new();
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of Blazor Server code generation
    /// </summary>
    public partial class BlazorServerResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<BlazorComponent> Components { get; set; } = new();
        public List<BlazorPage> Pages { get; set; } = new();
        public List<BlazorService> Services { get; set; } = new();
        public BlazorConfiguration Configuration { get; set; } = new();
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of Blazor WebAssembly code generation
    /// </summary>
    public partial class BlazorWebAssemblyResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<BlazorComponent> Components { get; set; } = new();
        public List<BlazorPage> Pages { get; set; } = new();
        public List<BlazorService> Services { get; set; } = new();
        public BlazorConfiguration Configuration { get; set; } = new();
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of MAUI code generation
    /// </summary>
    public partial class MauiResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<MauiPage> Pages { get; set; } = new();
        public List<MauiView> Views { get; set; } = new();
        public List<MauiService> Services { get; set; } = new();
        public MauiConfiguration Configuration { get; set; } = new();
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of console application code generation
    /// </summary>
    public partial class ConsoleResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<ConsoleCommand> Commands { get; set; } = new();
        public List<ConsoleService> Services { get; set; } = new();
        public ConsoleConfiguration Configuration { get; set; } = new();
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of WPF application code generation
    /// </summary>
    public partial class WpfResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<WpfWindow> Windows { get; set; } = new();
        public List<WpfUserControl> UserControls { get; set; } = new();
        public List<WpfViewModel> ViewModels { get; set; } = new();
        public WpfConfiguration Configuration { get; set; } = new();
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of WinForms application code generation
    /// </summary>
    public partial class WinFormsResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<WinFormsForm> Forms { get; set; } = new();
        public List<WinFormsControl> Controls { get; set; } = new();
        public List<WinFormsService> Services { get; set; } = new();
        public WinFormsConfiguration Configuration { get; set; } = new();
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of Xamarin application code generation
    /// </summary>
    public partial class XamarinResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<XamarinPage> Pages { get; set; } = new();
        public List<XamarinView> Views { get; set; } = new();
        public List<XamarinService> Services { get; set; } = new();
        public XamarinConfiguration Configuration { get; set; } = new();
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
