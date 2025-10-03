using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Director.Avalonia.Services;
using Director.Avalonia.ViewModels;

namespace Director.Avalonia;

public partial class App : Application
{
    private IHost? _host;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Configure services
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .Build();

        // Get services
        var mainViewModel = _host.Services.GetRequiredService<MainViewModel>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Services
        services.AddSingleton<DirectorClient>();
        services.AddSingleton<TokenService>();
        services.AddSingleton<DiffService>();
        services.AddSingleton<NexoCommandService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<ConnectionViewModel>();
        services.AddTransient<LogsViewModel>();
        services.AddTransient<GatesViewModel>();
        services.AddTransient<ValidationViewModel>();
    }

    // OnExit method removed - not compatible with current Avalonia version
    // Cleanup will be handled by the application lifetime
}
