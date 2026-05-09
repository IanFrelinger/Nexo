using Avalonia.Controls;
using Avalonia.Interactivity;
using Nexo.Client;

namespace Nexo.Demos.Avalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnStatusClick(object? sender, RoutedEventArgs e)
    {
        var raw = BaseUrlBox.Text?.Trim();
        if (string.IsNullOrEmpty(raw))
        {
            OutputBox.Text = "Enter a base URL (e.g. http://localhost:5000).";
            return;
        }

        var baseUri = new Uri(raw.TrimEnd('/') + "/");
        using var http = new HttpClient { BaseAddress = baseUri, Timeout = TimeSpan.FromSeconds(60) };
        var client = new NexoClient(http);
        try
        {
            var status = await client.GetStatusAsync().ConfigureAwait(true);
            OutputBox.Text = System.Text.Json.JsonSerializer.Serialize(
                status,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            OutputBox.Text = ex.ToString();
        }
    }
}
