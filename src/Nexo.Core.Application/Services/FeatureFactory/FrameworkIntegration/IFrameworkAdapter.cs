using Nexo.Core.Domain.Entities.FeatureFactory;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.FeatureFactory.FrameworkIntegration
{
    /// <summary>
    /// Interface for adapting application logic to different frameworks.
    /// This interface acts as an orchestrator, delegating specific functionalities to partial interface implementations.
    /// </summary>
    public partial interface IFrameworkAdapter
    {
        /// <summary>
        /// Generates framework-specific code from application logic
        /// </summary>
        Task<FrameworkResult> GenerateFrameworkCodeAsync(ApplicationLogicResult applicationLogic, FrameworkType framework, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates ASP.NET Core Web API code
        /// </summary>
        Task<WebApiResult> GenerateWebApiCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates Blazor Server code
        /// </summary>
        Task<BlazorServerResult> GenerateBlazorServerCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates Blazor WebAssembly code
        /// </summary>
        Task<BlazorWebAssemblyResult> GenerateBlazorWebAssemblyCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates .NET MAUI mobile application code
        /// </summary>
        Task<MauiResult> GenerateMauiCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates console application code
        /// </summary>
        Task<ConsoleResult> GenerateConsoleCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates WPF desktop application code
        /// </summary>
        Task<WpfResult> GenerateWpfCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates WinForms desktop application code
        /// </summary>
        Task<WinFormsResult> GenerateWinFormsCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates Xamarin mobile application code
        /// </summary>
        Task<XamarinResult> GenerateXamarinCodeAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default);
    }
}