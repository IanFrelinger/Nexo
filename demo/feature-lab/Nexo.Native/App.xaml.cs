using Nexo.Native.Views;

namespace Nexo.Native;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		MainPage = new LovableDemoPage();
	}
}
