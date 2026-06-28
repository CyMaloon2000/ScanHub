using Microsoft.Extensions.DependencyInjection;
using InventoryScanner.Views;

namespace InventoryScanner;

public partial class App : Application
{
	private readonly IServiceProvider _services;

	public App(IServiceProvider services)
	{
		_services = services;
		InitializeComponent();

		// Load and apply user's saved theme preference
		var theme = Preferences.Get("theme_preference", "System");
		UserAppTheme = theme switch
		{
			"Light" => AppTheme.Light,
			"Dark" => AppTheme.Dark,
			_ => AppTheme.Unspecified
		};
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(_services.GetRequiredService<AppShell>());
	}
}
