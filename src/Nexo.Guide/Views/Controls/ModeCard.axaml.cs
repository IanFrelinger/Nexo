using Avalonia;
using Avalonia.Controls;

namespace Nexo.Guide.Views.Controls;

public partial class ModeCard : UserControl
{
	public static readonly StyledProperty<string> ModeProperty =
		AvaloniaProperty.Register<ModeCard, string>(nameof(Mode), "");
	public static readonly StyledProperty<string> DescriptionProperty =
		AvaloniaProperty.Register<ModeCard, string>(nameof(Description), "");

	public string Mode { get => GetValue(ModeProperty); set => SetValue(ModeProperty, value); }
	public string Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

	public ModeCard()
	{
		InitializeComponent();
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);
		if (e.Property == ModeProperty)
			ModeLabel.Text = (string?)e.NewValue ?? "";
		if (e.Property == DescriptionProperty)
			DescLabel.Text = (string?)e.NewValue ?? "";
	}

	protected override void OnDataContextChanged(EventArgs e)
	{
		base.OnDataContextChanged(e);
		ModeLabel.Text = Mode;
		DescLabel.Text = Description;
	}
}
