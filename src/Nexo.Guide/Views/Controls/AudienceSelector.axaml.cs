using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Nexo.Guide.Views.Controls;

public partial class AudienceSelector : UserControl
{
	public static readonly StyledProperty<string> SelectedAudienceProperty =
		AvaloniaProperty.Register<AudienceSelector, string>(nameof(SelectedAudience), "general");

	public string SelectedAudience { get => GetValue(SelectedAudienceProperty); set => SetValue(SelectedAudienceProperty, value); }

	public AudienceSelector()
	{
		InitializeComponent();
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);
		if (e.Property == SelectedAudienceProperty)
			UpdateStyles((string?)e.NewValue ?? "general");
	}

	void UpdateStyles(string selected)
	{
		var activeBg = new SolidColorBrush(Color.FromRgb(35, 35, 38));
		var activeFg = new SolidColorBrush(Color.FromRgb(245, 245, 247));
		var inactiveBg = Brushes.Transparent;
		var inactiveFg = new SolidColorBrush(Color.FromArgb(77, 255, 255, 255));

		BtnGeneral.Background = selected == "general" ? activeBg : inactiveBg;
		BtnGeneral.Foreground = selected == "general" ? activeFg : inactiveFg;
		BtnDeveloper.Background = selected == "developer" ? activeBg : inactiveBg;
		BtnDeveloper.Foreground = selected == "developer" ? activeFg : inactiveFg;
		BtnDecision.Background = selected == "decision-maker" ? activeBg : inactiveBg;
		BtnDecision.Foreground = selected == "decision-maker" ? activeFg : inactiveFg;
	}

	void OnPillClicked(object? sender, RoutedEventArgs e)
	{
		var btn = (Button)sender!;
		SelectedAudience = btn == BtnGeneral ? "general" : btn == BtnDeveloper ? "developer" : "decision-maker";
	}
}
