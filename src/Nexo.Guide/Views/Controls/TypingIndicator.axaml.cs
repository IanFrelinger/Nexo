using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Nexo.Guide.Views.Controls;

public partial class TypingIndicator : UserControl
{
	private DispatcherTimer? _timer;
	private int _animIndex;

	public TypingIndicator()
	{
		InitializeComponent();
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);
		if (e.Property == IsVisibleProperty)
		{
			if (IsVisible) StartAnimation();
			else StopAnimation();
		}
	}

	private void StartAnimation()
	{
		_animIndex = 0;
		var dots = new[] { Dot1, Dot2, Dot3 };
		Dot1.Opacity = Dot2.Opacity = Dot3.Opacity = 0.25;
		Dot1.Opacity = 0.9;
		_timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
		_timer.Tick += (_, _) =>
		{
			dots[_animIndex % 3].Opacity = 0.25;
			_animIndex++;
			dots[_animIndex % 3].Opacity = 0.9;
		};
		_timer.Start();
	}

	private void StopAnimation()
	{
		_timer?.Stop();
		_timer = null;
		Dot1.Opacity = Dot2.Opacity = Dot3.Opacity = 0.25;
	}
}
