using Playground.Maui.ViewModels;

namespace Playground.Maui.Pages;

public partial class FeatureFactoryPage : ContentPage
{
    public FeatureFactoryPage(FeatureFactoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
