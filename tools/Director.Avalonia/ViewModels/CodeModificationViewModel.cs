using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Director.Avalonia.Services;
using Microsoft.Extensions.Logging;

namespace Director.Avalonia.ViewModels;

/// <summary>
/// ViewModel for code modification and hot reload functionality
/// </summary>
public partial class CodeModificationViewModel : ObservableObject
{
    private readonly ILogger<CodeModificationViewModel> _logger;
    private readonly CodeModificationService _codeModificationService;
    private readonly NaturalLanguageProcessor _naturalLanguageProcessor;
    private readonly HotReloadService _hotReloadService;

    [ObservableProperty]
    private string _naturalLanguagePrompt = string.Empty;

    [ObservableProperty]
    private string _targetClass = "MainViewModel";

    [ObservableProperty]
    private string _targetMethod = "ProcessCommand";

    [ObservableProperty]
    private string _generatedCode = string.Empty;

    [ObservableProperty]
    private string _currentImplementation = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isProcessing = false;

    [ObservableProperty]
    private bool _isValidating = false;

    [ObservableProperty]
    private bool _isReloading = false;

    public ObservableCollection<CodeModificationResult> ModificationHistory { get; } = new();
    public ObservableCollection<CodeSuggestion> Suggestions { get; } = new();
    public ObservableCollection<string> LoadedComponents { get; } = new();

    public ICommand ProcessPromptCommand { get; }
    public ICommand GenerateCodeCommand { get; }
    public ICommand ValidateCodeCommand { get; }
    public ICommand ApplyHotReloadCommand { get; }
    public ICommand GetCurrentImplementationCommand { get; }
    public ICommand SuggestModificationsCommand { get; }
    public ICommand ClearHistoryCommand { get; }

    public CodeModificationViewModel(
        ILogger<CodeModificationViewModel> logger,
        CodeModificationService codeModificationService,
        NaturalLanguageProcessor naturalLanguageProcessor,
        HotReloadService hotReloadService)
    {
        _logger = logger;
        _codeModificationService = codeModificationService;
        _naturalLanguageProcessor = naturalLanguageProcessor;
        _hotReloadService = hotReloadService;

        // Initialize commands
        ProcessPromptCommand = new AsyncRelayCommand(ProcessPromptAsync);
        GenerateCodeCommand = new AsyncRelayCommand(GenerateCodeAsync);
        ValidateCodeCommand = new AsyncRelayCommand(ValidateCodeAsync);
        ApplyHotReloadCommand = new AsyncRelayCommand(ApplyHotReloadAsync);
        GetCurrentImplementationCommand = new AsyncRelayCommand(GetCurrentImplementationAsync);
        SuggestModificationsCommand = new AsyncRelayCommand(SuggestModificationsAsync);
        ClearHistoryCommand = new RelayCommand(ClearHistory);

        // Subscribe to hot reload events
        _hotReloadService.ComponentReloaded += OnComponentReloaded;
        _hotReloadService.ReloadError += OnReloadError;

        // Load initial data
        LoadInitialData();
    }

    private async Task ProcessPromptAsync()
    {
        if (string.IsNullOrWhiteSpace(NaturalLanguagePrompt))
        {
            StatusMessage = "Please enter a natural language prompt";
            return;
        }

        try
        {
            IsProcessing = true;
            StatusMessage = "Processing natural language prompt...";

            var result = await _naturalLanguageProcessor.ProcessPromptAsync(
                NaturalLanguagePrompt, 
                TargetClass, 
                TargetMethod);

            if (result.Success)
            {
                GeneratedCode = result.GeneratedCode;
                ModificationHistory.Add(result);
                StatusMessage = "Prompt processed successfully";
            }
            else
            {
                StatusMessage = $"Failed to process prompt: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process prompt");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async Task GenerateCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(NaturalLanguagePrompt))
        {
            StatusMessage = "Please enter a natural language prompt";
            return;
        }

        try
        {
            IsProcessing = true;
            StatusMessage = "Generating code...";

            var result = await _naturalLanguageProcessor.ProcessPromptAsync(
                NaturalLanguagePrompt, 
                TargetClass, 
                TargetMethod);

            if (result.Success)
            {
                GeneratedCode = result.GeneratedCode;
                StatusMessage = "Code generated successfully";
            }
            else
            {
                StatusMessage = $"Code generation failed: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate code");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async Task ValidateCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(GeneratedCode))
        {
            StatusMessage = "No code to validate";
            return;
        }

        try
        {
            IsValidating = true;
            StatusMessage = "Validating code...";

            var validationResult = await _codeModificationService.ValidateCodeAsync(
                GeneratedCode, 
                TargetClass, 
                TargetMethod);

            if (validationResult.IsValid)
            {
                StatusMessage = "Code validation passed";
            }
            else
            {
                var errors = string.Join(", ", validationResult.Errors);
                StatusMessage = $"Code validation failed: {errors}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate code");
            StatusMessage = $"Validation error: {ex.Message}";
        }
        finally
        {
            IsValidating = false;
        }
    }

    private async Task ApplyHotReloadAsync()
    {
        if (string.IsNullOrWhiteSpace(GeneratedCode))
        {
            StatusMessage = "No code to apply";
            return;
        }

        try
        {
            IsReloading = true;
            StatusMessage = "Applying hot reload...";

            var success = await _hotReloadService.ReloadComponentAsync(
                $"{TargetClass}_{TargetMethod}",
                GeneratedCode,
                TargetClass,
                TargetMethod);

            if (success)
            {
                StatusMessage = "Hot reload applied successfully";
                LoadedComponents.Clear();
                foreach (var component in _hotReloadService.GetLoadedComponents())
                {
                    LoadedComponents.Add(component);
                }
            }
            else
            {
                StatusMessage = "Hot reload failed";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply hot reload");
            StatusMessage = $"Hot reload error: {ex.Message}";
        }
        finally
        {
            IsReloading = false;
        }
    }

    private async Task GetCurrentImplementationAsync()
    {
        try
        {
            StatusMessage = "Getting current implementation...";

            CurrentImplementation = await _codeModificationService.GetCurrentImplementationAsync(
                TargetClass, 
                TargetMethod);

            StatusMessage = "Current implementation retrieved";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current implementation");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private async Task SuggestModificationsAsync()
    {
        if (string.IsNullOrWhiteSpace(NaturalLanguagePrompt))
        {
            StatusMessage = "Please enter a natural language prompt";
            return;
        }

        try
        {
            StatusMessage = "Generating suggestions...";

            var suggestions = await _naturalLanguageProcessor.SuggestModificationsAsync(
                NaturalLanguagePrompt, 
                CurrentImplementation);

            Suggestions.Clear();
            foreach (var suggestion in suggestions)
            {
                Suggestions.Add(suggestion);
            }

            StatusMessage = $"Generated {suggestions.Count} suggestions";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate suggestions");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private void ClearHistory()
    {
        ModificationHistory.Clear();
        Suggestions.Clear();
        StatusMessage = "History cleared";
    }

    private void LoadInitialData()
    {
        // Load existing components
        foreach (var component in _hotReloadService.GetLoadedComponents())
        {
            LoadedComponents.Add(component);
        }
    }

    private void OnComponentReloaded(object? sender, HotReloadEventArgs e)
    {
        global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            StatusMessage = $"Component {e.ComponentName} reloaded successfully";
            _logger.LogInformation($"Component {e.ComponentName} reloaded successfully");
        });
    }

    private void OnReloadError(object? sender, HotReloadErrorEventArgs e)
    {
        global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            StatusMessage = $"Reload error for {e.ComponentName}: {e.ErrorMessage}";
            _logger.LogError($"Reload error for {e.ComponentName}: {e.ErrorMessage}");
        });
    }
}
