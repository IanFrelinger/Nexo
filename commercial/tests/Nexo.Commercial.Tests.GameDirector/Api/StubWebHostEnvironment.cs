using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Nexo.Tests.GameDirector.Api;
/// <summary>Stub web host environment.</summary>
internal sealed class StubWebHostEnvironment : IWebHostEnvironment
{
    /// <summary>Application name.</summary>
    public string ApplicationName { get; set; } = "test";
    /// <summary>Content root file provider.</summary>
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    /// <summary>Content root path.</summary>
    public string ContentRootPath { get; set; } = Path.GetTempPath();
    /// <summary>Environment name.</summary>
    public string EnvironmentName { get; set; } = "Production";
    /// <summary>Web root path.</summary>
    public string WebRootPath { get; set; } = Path.GetTempPath();
    /// <summary>Web root file provider.</summary>
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}
