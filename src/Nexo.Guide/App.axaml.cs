using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Common.Services;
using Nexo.Core.Domain.Agents;
using Nexo.Guide.Agent.Behaviors;
using Nexo.Guide.Bricks;
using Nexo.Guide.Knowledge;
using Nexo.Guide.Ollama;
using Nexo.Guide.Rendering;
using Nexo.Guide.Rendering.Backends;
using Nexo.Guide.Sandbox;
using Nexo.Guide.Services;
using Nexo.Guide.Unity;
using Nexo.Guide.ViewModels;
using Nexo.Infrastructure.Execution;

namespace Nexo.Guide;

public partial class App : Application
{
	public override void Initialize() => AvaloniaXamlLoader.Load(this);

	public override void OnFrameworkInitializationCompleted()
	{
		var services = new ServiceCollection();

		services.AddSingleton<IOllamaClient>(_ =>
		{
			var baseUrl = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL") ?? "http://localhost:11434";
			var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "llama3.1:8b";
			return new OllamaClient(baseUrl, model);
		});
		services.AddSingleton<NexoKnowledgeBase>();

		services.AddSingleton<IProviderFactory, ProviderFactory>();
		services.AddSingleton<ILoopKernel, SequentialLoopKernel>();
		services.AddSingleton<ISemanticCache, SemanticCache>();
		services.AddSingleton<Nexo.Core.Domain.Execution.IBrickRegistry>(sp =>
		{
			var ollama = sp.GetRequiredService<IOllamaClient>();
			var knowledge = sp.GetRequiredService<NexoKnowledgeBase>();
			return new BrickRegistry(new Nexo.Core.Domain.Bricks.Brick[] { new GuideChatBrick(ollama, knowledge) });
		});
		services.AddSingleton<Nexo.Core.Domain.Execution.IBehaviorRegistry>(sp =>
			new BehaviorRegistry(new[] { GuideNexoSetup.CreateGuideChatBehavior() }));
		services.AddSingleton<Nexo.Core.Domain.Execution.IBehaviorExecutor, BehaviorExecutor>();

		services.AddSingleton<AgentCard>(_ => GuideNexoSetup.CreateAgentCard());
		services.AddSingleton(_ => new SandboxPolicy
		{
			MaxBricksPerTurn = 50,
			TurnTimeout = TimeSpan.FromSeconds(10),
			MaxSceneElements = 200,
			MaxTotalVertices = 50_000,
			MaxTotalParticles = 500
		});
		services.AddSingleton<SandboxedExecutionEngine>();
		services.AddSingleton<ElementRegistry>();
		services.AddSingleton<IRenderSurface2D, SkiaRenderSurface>();
		services.AddSingleton<IRenderSurface3D>(sp => new ThreeJsRenderSurface(sp.GetService<IUnitySceneSink>()));
		services.AddSingleton<IUnitySceneSink, InMemoryUnitySceneSink>();
		services.AddSingleton<RenderBehavior>();
		services.AddSingleton<GuideService>();
		services.AddSingleton<MainViewModel>();

		// Required for ProviderFactory and other services that take ILogger<T>
		services.AddLogging(b =>
		{
			b.AddDebug();
			b.AddConsole();
		});

		ServiceProvider = services.BuildServiceProvider();

		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			var main = new MainWindow
			{
				DataContext = ServiceProvider.GetRequiredService<MainViewModel>()
			};
			main.Closed += (_, _) =>
			{
				ServiceProvider.GetService<GuideService>()?.ClearSession();
				ServiceProvider.GetService<ElementRegistry>()?.Clear();
				if (ServiceProvider.GetService<IRenderSurface2D>() is { } r2) r2.Clear();
				if (ServiceProvider.GetService<IRenderSurface3D>() is { } r3) r3.Clear();
				if (ServiceProvider.GetService<IUnitySceneSink>() is { } s) s.Clear();
			};
			desktop.MainWindow = main;

			if (Program.IsHeadless)
			{
				Dispatcher.UIThread.Post(() =>
				{
					var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
					timer.Tick += (_, _) =>
					{
						timer.Stop();
						desktop.Shutdown(0);
					};
					timer.Start();
				}, DispatcherPriority.Background);
			}
		}

		base.OnFrameworkInitializationCompleted();
	}

	internal static IServiceProvider? ServiceProvider { get; private set; }
}
