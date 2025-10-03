using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Director.Avalonia.Services;
using Director.Core.Protocol;

namespace Director.Avalonia.ViewModels;

public partial class ValidationViewModel : ObservableObject
{
    private readonly NexoCommandService _nexoCommandService;

    [ObservableProperty]
    private bool _isValidating = false;

    [ObservableProperty]
    private string _validationStatus = "Ready";

    [ObservableProperty]
    private string _lastValidationResult = string.Empty;

    public ICommand RunValidationCommand { get; }
    public ICommand RunAnalysisCommand { get; }

    public ValidationViewModel(NexoCommandService nexoCommandService)
    {
        _nexoCommandService = nexoCommandService;

        RunValidationCommand = new AsyncRelayCommand(RunValidationAsync);
        RunAnalysisCommand = new AsyncRelayCommand(RunAnalysisAsync);
    }

    private async Task RunValidationAsync()
    {
        IsValidating = true;
        ValidationStatus = "Running validation...";
        
        try
        {
            var command = _nexoCommandService.CreateValidationCommand();
            // TODO: Send command through DirectorClient
            LastValidationResult = "Validation completed";
        }
        catch (Exception ex)
        {
            LastValidationResult = $"Validation failed: {ex.Message}";
        }
        finally
        {
            IsValidating = false;
            ValidationStatus = "Ready";
        }
    }

    private async Task RunAnalysisAsync()
    {
        IsValidating = true;
        ValidationStatus = "Running analysis...";
        
        try
        {
            var command = _nexoCommandService.CreateAnalysisCommand();
            // TODO: Send command through DirectorClient
            LastValidationResult = "Analysis completed";
        }
        catch (Exception ex)
        {
            LastValidationResult = $"Analysis failed: {ex.Message}";
        }
        finally
        {
            IsValidating = false;
            ValidationStatus = "Ready";
        }
    }
}
