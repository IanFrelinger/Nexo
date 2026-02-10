using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Nexo.Guide.Views.Controls;

public partial class LayerCard : UserControl
{
	public static readonly StyledProperty<string> LayerNameProperty =
		AvaloniaProperty.Register<LayerCard, string>(nameof(LayerName), "");
	public static readonly StyledProperty<string> LayerColorProperty =
		AvaloniaProperty.Register<LayerCard, string>(nameof(LayerColor), "#FFFFFF");
	public static readonly StyledProperty<string> DescriptionProperty =
		AvaloniaProperty.Register<LayerCard, string>(nameof(Description), "");
	public static readonly StyledProperty<double> BackgroundOpacityProperty =
		AvaloniaProperty.Register<LayerCard, double>(nameof(BackgroundOpacity), 0.04);

	public string LayerName { get => GetValue(LayerNameProperty); set => SetValue(LayerNameProperty, value); }
	public string LayerColor { get => GetValue(LayerColorProperty); set => SetValue(LayerColorProperty, value); }
	public string Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
	public double BackgroundOpacity { get => GetValue(BackgroundOpacityProperty); set => SetValue(BackgroundOpacityProperty, value); }

	private bool _expanded;

	public LayerCard()
	{
		InitializeComponent();
		Container.PointerPressed += OnTapped;
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);
		if (e.Property == LayerNameProperty)
			NameLabel.Text = (string?)e.NewValue ?? "";
		if (e.Property == DescriptionProperty)
			DescLabel.Text = (string?)e.NewValue ?? "";
		if (e.Property == LayerColorProperty || e.Property == BackgroundOpacityProperty)
			UpdateColor();
	}

	protected override void OnDataContextChanged(EventArgs e)
	{
		base.OnDataContextChanged(e);
		NameLabel.Text = LayerName;
		DescLabel.Text = Description;
		UpdateColor();
	}

	private void UpdateColor()
	{
		var color = ParseColor(LayerColor);
		Dot.Fill = new SolidColorBrush(color);
		Container.Background = new SolidColorBrush(Color.FromArgb((byte)(BackgroundOpacity * 255), color.R, color.G, color.B));
	}

	private static Color ParseColor(string hex)
	{
		if (hex.StartsWith("#") && hex.Length >= 7)
		{
			var r = Convert.ToByte(hex.Substring(1, 2), 16);
			var g = Convert.ToByte(hex.Substring(3, 2), 16);
			var b = Convert.ToByte(hex.Substring(5, 2), 16);
			return Color.FromRgb(r, g, b);
		}
		return Colors.White;
	}

	private void OnTapped(object? sender, RoutedEventArgs e)
	{
		_expanded = !_expanded;
		DescLabel.IsVisible = _expanded;
		Container.BorderBrush = _expanded ? new SolidColorBrush(Color.FromArgb(26, 255, 255, 255)) : Brushes.Transparent;
	}
}
