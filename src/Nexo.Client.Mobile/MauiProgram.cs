using Microsoft.Extensions.Logging;
using Nexo.Client;

namespace Nexo.Client.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var baseUrl = Preferences.Default.Get("NexoServerUrl", "http://localhost:5000");
		builder.Services.AddNexoClient(baseUrl);
		builder.Services.AddTransient<MainPage>();

		return builder.Build();
	}
}
