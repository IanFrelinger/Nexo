using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Director.Avalonia.Services;
using Director.Core.Protocol;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Avalonia.Media;
using System.Text.Json;

namespace Director.Avalonia.ViewModels;

public partial class EnhancedMainViewModel : ObservableObject
{
    private readonly DirectorClient _directorClient;
    private readonly TokenService _tokenService;
    private readonly NexoCommandService _nexoCommandService;
    private readonly SystemSettingsService _systemSettingsService;
    private readonly UnityDiscoveryService _unityDiscoveryService;
    private readonly ILogger<EnhancedMainViewModel> _logger;

    [ObservableProperty]
    private string _connectionStatus = "Disconnected";

    [ObservableProperty]
    private bool _isConnected = false;

    [ObservableProperty]
    private string _token = string.Empty;

    [ObservableProperty]
    private string _targetSlot = "quality.filters";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _lastActivity = string.Empty;

    [ObservableProperty]
    private string _unityStatus = "Click 'Auto-Discover Unity' to find installations";

    [ObservableProperty]
    private FontFamily _systemFontFamily = FontFamily.Default;

    [ObservableProperty]
    private double _systemFontSize = 14.0;

    [ObservableProperty]
    private Color _systemAccentColor = Color.FromRgb(0, 120, 215);

    [ObservableProperty]
    private SystemTheme _systemTheme = SystemTheme.Light;

    public ObservableCollection<LogLineEvent> Logs { get; } = new();
    public ObservableCollection<GateResultEvent> Gates { get; } = new();
    public ObservableCollection<UnityInstallation> UnityInstallations { get; } = new();

    [ObservableProperty]
    private UnityInstallation? _selectedUnityInstallation;

    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand AutoDiscoverTokenCommand { get; }
    public ICommand AutoDiscoverUnityCommand { get; }
    public ICommand AutoConnectCommand { get; }
    public ICommand RunValidationCommand { get; }
    public ICommand RunAnalysisCommand { get; }
    public ICommand ListAgentsCommand { get; }
    public ICommand TogglePlayCommand { get; }
    public ICommand GetProjectInfoCommand { get; }
    public ICommand ListScenesCommand { get; }
    public ICommand ApplyUIModCommand { get; }

    public EnhancedMainViewModel(
        DirectorClient directorClient,
        TokenService tokenService,
        NexoCommandService nexoCommandService,
        SystemSettingsService systemSettingsService,
        UnityDiscoveryService unityDiscoveryService,
        ILogger<EnhancedMainViewModel> logger)
    {
        _directorClient = directorClient;
        _tokenService = tokenService;
        _nexoCommandService = nexoCommandService;
        _systemSettingsService = systemSettingsService;
        _unityDiscoveryService = unityDiscoveryService;
        _logger = logger;

        // Initialize commands
        ConnectCommand = new AsyncRelayCommand(ConnectAsync);
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync);
        AutoDiscoverTokenCommand = new RelayCommand(AutoDiscoverToken);
        AutoDiscoverUnityCommand = new AsyncRelayCommand(AutoDiscoverUnityAsync);
        AutoConnectCommand = new AsyncRelayCommand(AutoConnectAsync);
        RunValidationCommand = new AsyncRelayCommand(RunValidationAsync);
        RunAnalysisCommand = new AsyncRelayCommand(RunAnalysisAsync);
        ListAgentsCommand = new AsyncRelayCommand(ListAgentsAsync);
        TogglePlayCommand = new AsyncRelayCommand(TogglePlayAsync);
        GetProjectInfoCommand = new AsyncRelayCommand(GetProjectInfoAsync);
        ListScenesCommand = new AsyncRelayCommand(ListScenesAsync);
        ApplyUIModCommand = new AsyncRelayCommand(ApplyUIModAsync);

        // Subscribe to events
        _directorClient.EventReceived += OnEventReceived;
        _directorClient.ConnectionStatusChanged += OnConnectionStatusChanged;

        // Initialize system settings
        InitializeSystemSettings();

        // Auto-discover on startup
        _ = Task.Run(async () =>
        {
            await AutoDiscoverUnityAsync();
            AutoDiscoverToken();
        });
    }

    private void InitializeSystemSettings()
    {
        try
        {
            SystemFontFamily = _systemSettingsService.GetSystemFontFamily();
            SystemFontSize = _systemSettingsService.GetSystemFontSize();
            SystemAccentColor = _systemSettingsService.GetSystemAccentColor();
            SystemTheme = _systemSettingsService.GetSystemTheme();

            _logger.LogInformation($"System settings initialized - Theme: {SystemTheme}, Font: {SystemFontFamily.Name}, Size: {SystemFontSize}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize system settings");
        }
    }

    private async Task AutoDiscoverUnityAsync()
    {
        try
        {
            UnityStatus = "Discovering Unity installations...";
            
            var installations = await _unityDiscoveryService.DiscoverUnityInstallationsAsync();
            
            await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                UnityInstallations.Clear();
                foreach (var installation in installations)
                {
                    UnityInstallations.Add(installation);
                }

                if (installations.Any())
                {
                    var recommended = _unityDiscoveryService.GetRecommendedInstallation();
                    SelectedUnityInstallation = recommended ?? installations.First();
                    UnityStatus = $"Found {installations.Count} Unity installation(s)";
                }
                else
                {
                    UnityStatus = "No Unity installations found";
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover Unity installations");
            UnityStatus = "Failed to discover Unity installations";
        }
    }

    private async Task AutoConnectAsync()
    {
        try
        {
            // First discover Unity if not already done
            if (!UnityInstallations.Any())
            {
                await AutoDiscoverUnityAsync();
            }

            // Auto-discover token
            AutoDiscoverToken();

            // Connect if we have both Unity and token
            if (SelectedUnityInstallation != null && !string.IsNullOrEmpty(Token))
            {
                await ConnectAsync();
            }
            else
            {
                StatusMessage = "Please ensure Unity is discovered and token is available";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-connect");
            StatusMessage = "Auto-connect failed";
        }
    }

    private async Task ConnectAsync()
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            StatusMessage = "Please enter a token";
            return;
        }

        StatusMessage = "Connecting...";
        var success = await _directorClient.ConnectAsync(Token);
        
        if (success)
        {
            StatusMessage = "Connected to Unity Editor";
            LastActivity = DateTime.Now.ToString("HH:mm:ss");
        }
        else
        {
            StatusMessage = "Failed to connect";
        }
    }

    private async Task DisconnectAsync()
    {
        await _directorClient.DisconnectAsync();
        StatusMessage = "Disconnected";
    }

    private void AutoDiscoverToken()
    {
        var discoveredToken = _tokenService.DiscoverToken();
        if (!string.IsNullOrEmpty(discoveredToken))
        {
            Token = discoveredToken;
            StatusMessage = "Token auto-discovered";
        }
        else
        {
            StatusMessage = "No token found - start Unity Editor first";
        }
    }

    private async Task RunValidationAsync()
    {
        var command = _nexoCommandService.CreateValidationCommand();
        await _directorClient.SendCommandAsync(command);
        StatusMessage = "Running validation...";
    }

    private async Task RunAnalysisAsync()
    {
        var command = _nexoCommandService.CreateAnalysisCommand();
        await _directorClient.SendCommandAsync(command);
        StatusMessage = "Running analysis...";
    }

    private async Task ListAgentsAsync()
    {
        var command = _nexoCommandService.CreateListAgentsCommand();
        await _directorClient.SendCommandAsync(command);
        StatusMessage = "Listing agents...";
    }

    private async Task TogglePlayAsync()
    {
        var command = new DirectorCommand(
            Guid.NewGuid().ToString("N"),
            CommandTypes.TogglePlay,
            new TogglePlayPayload()
        );
        await _directorClient.SendCommandAsync(command);
        StatusMessage = "Toggling play mode...";
    }

    private async Task GetProjectInfoAsync()
    {
        var command = new DirectorCommand(
            Guid.NewGuid().ToString("N"),
            CommandTypes.GetProjectInfo,
            new GetProjectInfoPayload()
        );
        await _directorClient.SendCommandAsync(command);
        StatusMessage = "Getting project info...";
    }

    private async Task ListScenesAsync()
    {
        var command = new DirectorCommand(
            Guid.NewGuid().ToString("N"),
            CommandTypes.ListScenes,
            new ListScenesPayload()
        );
        await _directorClient.SendCommandAsync(command);
        StatusMessage = "Listing scenes...";
    }

    private async Task ApplyUIModAsync()
    {
        var uiSchema = new
        {
            elements = new object[]
            {
                new
                {
                    type = "toolbarMenu",
                    text = "Status: All",
                    items = new[] { "All", "Errors", "Warnings" }
                },
                new
                {
                    type = "toolbarToggle",
                    text = "Show Performance",
                    value = true
                }
            }
        };

        var command = new DirectorCommand(
            Guid.NewGuid().ToString("N"),
            CommandTypes.ApplyUIMod,
            new ApplyUIModPayload(TargetSlot, JsonSerializer.SerializeToElement(uiSchema))
        );
        await _directorClient.SendCommandAsync(command);
        StatusMessage = $"Applying UI mod to slot: {TargetSlot}";
    }

    private void OnEventReceived(object? sender, DirectorEvent evt)
    {
        switch (evt.Type)
        {
            case "LogLine":
                if (evt.Payload is LogLineEvent logEvent)
                {
                    global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Logs.Add(logEvent);
                        LastActivity = DateTime.Now.ToString("HH:mm:ss");
                    });
                }
                break;

            case "GateResult":
                if (evt.Payload is GateResultEvent gateEvent)
                {
                    global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Gates.Add(gateEvent);
                        LastActivity = DateTime.Now.ToString("HH:mm:ss");
                    });
                }
                break;

            case "RunFinished":
                if (evt.Payload is RunFinishedEvent runEvent)
                {
                    global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        StatusMessage = runEvent.ExitCode == 0 ? "Command completed successfully" : $"Command failed (exit code: {runEvent.ExitCode})";
                        LastActivity = DateTime.Now.ToString("HH:mm:ss");
                    });
                }
                break;
        }
    }

    private void OnConnectionStatusChanged(object? sender, string status)
    {
        global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            ConnectionStatus = status;
            IsConnected = status == "Connected";
            LastActivity = DateTime.Now.ToString("HH:mm:ss");
        });
    }
}
