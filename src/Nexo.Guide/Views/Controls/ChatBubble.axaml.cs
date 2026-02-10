using Avalonia.Controls;

namespace Nexo.Guide.Views.Controls;

public partial class ChatBubble : UserControl
{
	public ChatBubble()
	{
		InitializeComponent();
		DataContextChanged += (_, _) =>
		{
			if (DataContext is ViewModels.ChatMessage msg)
			{
				AssistantText.Text = msg.Text;
				MetricsText.Text = msg.HasMetrics
					? $"⚙️ {msg.BrickCount ?? 0} bricks · {msg.InferenceMs ?? 0}ms · local"
					: "";
			}
		};
	}
}
