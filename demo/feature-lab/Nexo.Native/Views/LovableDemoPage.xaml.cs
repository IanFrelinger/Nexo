using Nexo.Native.ViewModels;

namespace Nexo.Native.Views;

public partial class LovableDemoPage : ContentPage
{
    public LovableDemoPage()
    {
        InitializeComponent();
        BindingContext = new LovableDemoViewModel();
    }
}
