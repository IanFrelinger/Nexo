using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Director.Avalonia.ViewModels;

namespace Director.Avalonia.Views;

public partial class CodeModificationView : UserControl
{
    public CodeModificationView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
