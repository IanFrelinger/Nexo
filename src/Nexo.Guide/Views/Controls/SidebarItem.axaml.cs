using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Nexo.Guide.Views.Controls;

public partial class SidebarItem : UserControl
{
	public static readonly StyledProperty<string> IconProperty =
		AvaloniaProperty.Register<SidebarItem, string>(nameof(Icon), "○");
	public static readonly StyledProperty<string> TextProperty =
		AvaloniaProperty.Register<SidebarItem, string>(nameof(Text), "");
	public static readonly StyledProperty<bool> IsActiveProperty =
		AvaloniaProperty.Register<SidebarItem, bool>(nameof(IsActive), false);
	public static readonly StyledProperty<ICommand?> CommandProperty =
		AvaloniaProperty.Register<SidebarItem, ICommand?>(nameof(Command));

	public string Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
	public string Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }
	public bool IsActive { get => GetValue(IsActiveProperty); set => SetValue(IsActiveProperty, value); }
	public ICommand? Command { get => GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

	public SidebarItem()
	{
		InitializeComponent();
		PointerPressed += (_, e) => Command?.Execute(null);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);
		if (e.Property == IsActiveProperty)
			UpdateActive((bool)e.NewValue!);
		if (e.Property == IconProperty)
			IconLabel.Text = (string?)e.NewValue ?? "○";
		if (e.Property == TextProperty)
			TextLabel.Text = (string?)e.NewValue ?? "";
	}

	protected override void OnDataContextChanged(EventArgs e)
	{
		base.OnDataContextChanged(e);
		UpdateActive(IsActive);
		IconLabel.Text = Icon;
		TextLabel.Text = Text;
	}

	void UpdateActive(bool active)
	{
		Container.Background = active ? new SolidColorBrush(Color.FromRgb(255, 255, 255), 0.07) : Brushes.Transparent;
		TextLabel.Foreground = active ? new SolidColorBrush(Color.FromRgb(245, 245, 247)) : new SolidColorBrush(Color.FromRgb(140, 255, 255));
		IconLabel.Opacity = active ? 1.0 : 0.7;
	}
}
