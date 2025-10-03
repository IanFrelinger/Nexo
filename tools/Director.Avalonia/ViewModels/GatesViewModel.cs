using CommunityToolkit.Mvvm.ComponentModel;
using Director.Core.Protocol;
using System.Collections.ObjectModel;

namespace Director.Avalonia.ViewModels;

public partial class GatesViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<GateResultEvent> _gates = new();

    [ObservableProperty]
    private int _passedCount = 0;

    [ObservableProperty]
    private int _failedCount = 0;

    public void AddGateResult(GateResultEvent gateEvent)
    {
        Gates.Add(gateEvent);
        
        if (gateEvent.Passed)
        {
            PassedCount++;
        }
        else
        {
            FailedCount++;
        }
    }

    public void ClearGates()
    {
        Gates.Clear();
        PassedCount = 0;
        FailedCount = 0;
    }
}
