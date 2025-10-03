using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Director.Avalonia.Services;
using Director.Core.Protocol;
using System.Collections.ObjectModel;

namespace Director.Avalonia.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly DirectorClient _directorClient;
    private readonly TokenService _tokenService;
    private readonly NexoCommandService _nexoCommandService;

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

    public ObservableCollection<LogLineEvent> Logs { get; } = new();
    public ObservableCollection<GateResultEvent> Gates { get; } = new();

    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand AutoDiscoverTokenCommand { get; }
    public ICommand RunValidationCommand { get; }
    public ICommand RunAnalysisCommand { get; }
    public ICommand ListAgentsCommand { get; }
    public ICommand TogglePlayCommand { get; }
    public ICommand GetProjectInfoCommand { get; }
    public ICommand ListScenesCommand { get; }
    public ICommand ApplyUIModCommand { get; }

    public MainViewModel(
        DirectorClient directorClient,
        TokenService tokenService,
        NexoCommandService nexoCommandService)
    {
        _directorClient = directorClient;
        _tokenService = tokenService;
        _nexoCommandService = nexoCommandService;

        // Initialize commands
        ConnectCommand = new AsyncRelayCommand(ConnectAsync);
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync);
        AutoDiscoverTokenCommand = new RelayCommand(AutoDiscoverToken);
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

        // Auto-discover token on startup
        AutoDiscoverToken();
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
            elements = new[]
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
            new ApplyUIModPayload(TargetSlot, System.Text.Json.JsonSerializer.SerializeToElement(uiSchema))
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
                    App.Current?.Dispatcher.InvokeAsync(() =>
                    {
                        Logs.Add(logEvent);
                        LastActivity = DateTime.Now.ToString("HH:mm:ss");
                    });
                }
                break;

            case "GateResult":
                if (evt.Payload is GateResultEvent gateEvent)
                {
                    App.Current?.Dispatcher.InvokeAsync(() =>
                    {
                        Gates.Add(gateEvent);
                        LastActivity = DateTime.Now.ToString("HH:mm:ss");
                    });
                }
                break;

            case "RunFinished":
                if (evt.Payload is RunFinishedEvent runEvent)
                {
                    App.Current?.Dispatcher.InvokeAsync(() =>
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
        App.Current?.Dispatcher.InvokeAsync(() =>
        {
            ConnectionStatus = status;
            IsConnected = status == "Connected";
            LastActivity = DateTime.Now.ToString("HH:mm:ss");
        });
    }
}
