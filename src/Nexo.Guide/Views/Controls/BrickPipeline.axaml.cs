using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Nexo.Guide.ViewModels;

namespace Nexo.Guide.Views.Controls;

public partial class BrickPipeline : UserControl
{
	public static readonly StyledProperty<IList?> BricksProperty =
		AvaloniaProperty.Register<BrickPipeline, IList?>(nameof(Bricks));

	public IList? Bricks { get => GetValue(BricksProperty); set => SetValue(BricksProperty, value); }

	public BrickPipeline()
	{
		InitializeComponent();
		Container.ItemTemplate = new FuncDataTemplate<BrickInfo>((brick, _) =>
		{
			var row = new Border
			{
				Background = new SolidColorBrush(Color.FromRgb(22, 22, 22)),
				Padding = new Thickness(14, 10),
				Margin = new Thickness(0, 0, 0, 1)
			};
			var grid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(22, GridUnitType.Pixel),
					new ColumnDefinition(12, GridUnitType.Pixel),
					new ColumnDefinition(1, GridUnitType.Star),
					new ColumnDefinition(12, GridUnitType.Pixel),
					new ColumnDefinition(GridLength.Auto)
				}
			};

			var modeLabel = new TextBlock { FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(245, 245, 247)), HorizontalAlignment = HorizontalAlignment.Center };
			modeLabel.Bind(TextBlock.TextProperty, new Binding("Mode"));
			var nameLabel = new TextBlock { FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(245, 245, 247)) };
			nameLabel.Bind(TextBlock.TextProperty, new Binding("Name"));
			var purposeLabel = new TextBlock { FontSize = 11, Foreground = new SolidColorBrush(Color.FromArgb(77, 255, 255, 255)) };
			purposeLabel.Bind(TextBlock.TextProperty, new Binding("Purpose"));

			Grid.SetColumn(modeLabel, 0);
			Grid.SetColumn(nameLabel, 2);
			Grid.SetColumn(purposeLabel, 4);
			grid.Children.Add(modeLabel);
			grid.Children.Add(nameLabel);
			grid.Children.Add(purposeLabel);
			row.Child = grid;

			return row;
		});
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);
		if (e.Property == BricksProperty)
			Container.ItemsSource = Bricks;
	}

	protected override void OnDataContextChanged(EventArgs e)
	{
		base.OnDataContextChanged(e);
		Container.ItemsSource = Bricks;
	}
}
