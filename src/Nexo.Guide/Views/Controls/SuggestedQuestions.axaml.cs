using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;

namespace Nexo.Guide.Views.Controls;

public partial class SuggestedQuestions : UserControl
{
	public static readonly StyledProperty<IList?> QuestionsProperty =
		AvaloniaProperty.Register<SuggestedQuestions, IList?>(nameof(Questions));
	public static readonly StyledProperty<ICommand?> SendCommandProperty =
		AvaloniaProperty.Register<SuggestedQuestions, ICommand?>(nameof(SendCommand));

	public IList? Questions { get => GetValue(QuestionsProperty); set => SetValue(QuestionsProperty, value); }
	public ICommand? SendCommand { get => GetValue(SendCommandProperty); set => SetValue(SendCommandProperty, value); }

	public SuggestedQuestions()
	{
		InitializeComponent();
		Container.ItemTemplate = new FuncDataTemplate<string>((q, _) =>
		{
			var chip = new Border
			{
				Padding = new Thickness(12, 6),
				Margin = new Thickness(0, 0, 6, 6),
				Background = Brushes.Transparent,
				BorderBrush = new SolidColorBrush(Color.FromArgb(26, 255, 255, 255)),
				BorderThickness = new Thickness(0.5),
				CornerRadius = new CornerRadius(16),
				Child = new TextBlock { Text = q ?? "", FontSize = 12, Foreground = new SolidColorBrush(Color.FromArgb(77, 255, 255, 255)) }
			};
			chip.PointerPressed += (_, __) => SendCommand?.Execute(q);
			return chip;
		});
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);
		if (e.Property == QuestionsProperty)
			Container.ItemsSource = Questions;
	}

	protected override void OnDataContextChanged(EventArgs e)
	{
		base.OnDataContextChanged(e);
		Container.ItemsSource = Questions;
	}
}
