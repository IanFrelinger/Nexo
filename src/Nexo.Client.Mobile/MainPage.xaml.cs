using Nexo.Client;

namespace Nexo.Client.Mobile;

public partial class MainPage : ContentPage
{
    private readonly INexoClient _client;

    public MainPage(INexoClient client)
    {
        InitializeComponent();
        _client = client ?? throw new ArgumentNullException(nameof(client));
        ServerUrlEntry.Text = Preferences.Default.Get("NexoServerUrl", "http://localhost:5000");
    }

    private void OnSaveUrlClicked(object sender, EventArgs e)
    {
        var url = ServerUrlEntry.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(url))
        {
            Preferences.Default.Set("NexoServerUrl", url);
            DisplayAlert("Saved", "Server URL saved. Restart the app to use the new URL.", "OK");
        }
    }

    private async void OnStatusClicked(object sender, EventArgs e)
    {
        try
        {
            StatusLabel.Text = "Status: Checking...";
            var result = await _client.GetStatusAsync();
            StatusLabel.Text = $"Status: {result.Mode} - {result.Message}";
            ResultLabel.Text = $"Mode: {result.Mode}";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Status: Offline";
            ResultLabel.Text = $"Error: {ex.Message}";
        }
    }

    private async void OnValidateClicked(object sender, EventArgs e)
    {
        try
        {
            ResultLabel.Text = "Running validation...";
            var result = await _client.RunValidationAsync();
            ResultLabel.Text = $"Validation: {(result.Passed ? "Passed" : "Failed")}\n{result.Message}\nTests: {result.PassedTests}/{result.TotalTests}";
        }
        catch (Exception ex)
        {
            ResultLabel.Text = $"Error: {ex.Message}";
        }
    }

    private async void OnOrchestrateClicked(object sender, EventArgs e)
    {
        try
        {
            ResultLabel.Text = "Orchestrating...";
            var result = await _client.OrchestrateAsync(new OrchestrationRequest("Create a simple hello world console app"));
            ResultLabel.Text = $"Orchestration: {(result.Success ? "Success" : "Failed")}\n{result.Summary ?? "N/A"}";
        }
        catch (Exception ex)
        {
            ResultLabel.Text = $"Error: {ex.Message}";
        }
    }
}
