using CommunityToolkit.Mvvm.ComponentModel;
using Director.Core.Protocol;
using System.Collections.ObjectModel;

namespace Director.Avalonia.ViewModels;

public partial class LogsViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<LogLineEvent> _logs = new();

    [ObservableProperty]
    private string _filter = string.Empty;

    [ObservableProperty]
    private LogLevel _minLogLevel = LogLevel.Information;

    public void AddLog(LogLineEvent logEvent)
    {
        if (logEvent.Level >= MinLogLevel)
        {
            Logs.Add(logEvent);
        }
    }

    public void ClearLogs()
    {
        Logs.Clear();
    }

    partial void OnFilterChanged(string value)
    {
        // TODO: Implement filtering logic
    }

    partial void OnMinLogLevelChanged(LogLevel value)
    {
        // TODO: Implement log level filtering
    }
}
