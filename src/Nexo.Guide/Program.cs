using System.Linq;
using Avalonia;
using Avalonia.Headless;

namespace Nexo.Guide;

internal static class Program
{
	internal static bool IsHeadless { get; private set; }

	[STAThread]
	public static int Main(string[] args)
	{
		var filtered = args.Where(a => a != "--headless").ToArray();
		IsHeadless = args.Length != filtered.Length;
		return BuildAvaloniaApp().StartWithClassicDesktopLifetime(filtered);
	}

	public static AppBuilder BuildAvaloniaApp()
	{
		var builder = AppBuilder.Configure<App>()
			.WithInterFont()
			.LogToTrace();
		if (IsHeadless)
			builder = builder.UseHeadless(new AvaloniaHeadlessPlatformOptions());
		else
			builder = builder.UsePlatformDetect();
		return builder;
	}
}
