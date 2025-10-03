using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Director.Avalonia.Services;
using System.Windows.Input;

namespace Director.Avalonia.ViewModels;

public partial class ConnectionViewModel : ObservableObject
{
    private readonly DirectorClient _directorClient;
    private readonly TokenService _tokenService;

    [ObservableProperty]
    private string _token = string.Empty;

    [ObservableProperty]
    private bool _isConnected = false;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand AutoDiscoverTokenCommand { get; }

    public ConnectionViewModel(DirectorClient directorClient, TokenService tokenService)
    {
        _directorClient = directorClient;
        _tokenService = tokenService;

        ConnectCommand = new AsyncRelayCommand(ConnectAsync);
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync);
        AutoDiscoverTokenCommand = new RelayCommand(AutoDiscoverToken);

        _directorClient.ConnectionStatusChanged += OnConnectionStatusChanged;
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

    private void OnConnectionStatusChanged(object? sender, string status)
    {
        global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsConnected = status == "Connected";
            StatusMessage = status;
        });
    }
}
