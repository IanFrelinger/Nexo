using Playground.Maui.Models;
using Playground.Maui.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Playground.Maui.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly FeatureService _featureService;
    private string _selectedFeature = string.Empty;
    private string _selectedMode = "Off";
    private string _selectedProvider = "Local";
    private bool _isRunning;
    private RunResult? _lastRunResult;
    private ValidationResult? _validationResult;
    private bool _isValidating;

    public MainViewModel(FeatureService featureService)
    {
        _featureService = featureService;
        Features = new ObservableCollection<FeatureTemplate>(_featureService.GetAvailableFeatures());
        
        RunFeatureCommand = new Command(async () => await RunFeatureAsync());
        RunValidationCommand = new Command(async () => await RunValidationAsync());
        ClearResultsCommand = new Command(() => ClearResults());
        NavigateToFeatureFactoryCommand = new Command(async () => await NavigateToFeatureFactoryAsync());
    }

    public ObservableCollection<FeatureTemplate> Features { get; }

    public string SelectedFeature
    {
        get => _selectedFeature;
        set => SetProperty(ref _selectedFeature, value);
    }

    public string SelectedMode
    {
        get => _selectedMode;
        set => SetProperty(ref _selectedMode, value);
    }

    public string SelectedProvider
    {
        get => _selectedProvider;
        set => SetProperty(ref _selectedProvider, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        set => SetProperty(ref _isRunning, value);
    }

    public RunResult? LastRunResult
    {
        get => _lastRunResult;
        set => SetProperty(ref _lastRunResult, value);
    }

    public ValidationResult? ValidationResult
    {
        get => _validationResult;
        set => SetProperty(ref _validationResult, value);
    }

    public bool IsValidating
    {
        get => _isValidating;
        set => SetProperty(ref _isValidating, value);
    }

    public ICommand RunFeatureCommand { get; }
    public ICommand RunValidationCommand { get; }
    public ICommand ClearResultsCommand { get; }
    public ICommand NavigateToFeatureFactoryCommand { get; }

    private async Task RunFeatureAsync()
    {
        if (string.IsNullOrEmpty(SelectedFeature) || IsRunning)
            return;

        IsRunning = true;
        try
        {
            LastRunResult = await _featureService.RunFeatureAsync(SelectedFeature, SelectedMode, SelectedProvider);
        }
        finally
        {
            IsRunning = false;
        }
    }

    private async Task RunValidationAsync()
    {
        IsValidating = true;
        try
        {
            ValidationResult = await _featureService.RunValidationAsync();
        }
        finally
        {
            IsValidating = false;
        }
    }

    private void ClearResults()
    {
        LastRunResult = null;
        ValidationResult = null;
    }

    private async Task NavigateToFeatureFactoryAsync()
    {
        await Shell.Current.GoToAsync("//FeatureFactory");
    }
}
