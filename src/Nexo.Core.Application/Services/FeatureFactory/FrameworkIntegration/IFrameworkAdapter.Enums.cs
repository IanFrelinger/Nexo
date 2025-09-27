using Nexo.Core.Domain.Entities.FeatureFactory;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.FeatureFactory.FrameworkIntegration
{
    /// <summary>
    /// Enums for framework adapter functionality
    /// </summary>
    public partial interface IFrameworkAdapter
    {
        // Enums are defined here for framework adapter functionality
    }

    // Enums
    public enum FrameworkType
    {
        WebApi,
        BlazorServer,
        BlazorWebAssembly,
        Maui,
        Console,
        Wpf,
        WinForms,
        Xamarin
    }

    public enum FileType
    {
        Code,
        Configuration,
        Resource,
        Documentation
    }

    public enum DependencyType
    {
        NuGet,
        Npm,
        Maven,
        Gradle,
        CocoaPods
    }
}
