using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using ZXing.Net.Maui.Controls;
using InventoryScanner.Services;
using InventoryScanner.Services.Interfaces;
using InventoryScanner.Repositories;
using InventoryScanner.Repositories.Interfaces;
using InventoryScanner.ViewModels;
using InventoryScanner.Views;
#if ANDROID
using InventoryScanner.Platforms.Android;
#endif

namespace InventoryScanner;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.UseBarcodeReader()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Infrastructure
		builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
		builder.Services.AddSingleton<IEventAggregator, EventAggregator>();

		// Repositories
		builder.Services.AddSingleton<IBuildingRepository, BuildingRepository>();
		builder.Services.AddSingleton<ILocationRepository, LocationRepository>();
		builder.Services.AddSingleton<IRoomRepository, RoomRepository>();
		builder.Services.AddSingleton<IOfficeRepository, OfficeRepository>();
		builder.Services.AddSingleton<ISessionRepository, SessionRepository>();
		builder.Services.AddSingleton<IScannedItemRepository, ScannedItemRepository>();
		builder.Services.AddSingleton<IUserRepository, UserRepository>();

		// Services
		builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
		builder.Services.AddSingleton<ISetupService, SetupService>();
		builder.Services.AddSingleton<IRoomService, RoomService>();
		builder.Services.AddSingleton<ISessionService, SessionService>();
		builder.Services.AddSingleton<IScannerService, ScannerService>();
		builder.Services.AddSingleton<ZxingScannerService>();
		builder.Services.AddSingleton<GoogleMlKitScannerService>();
		builder.Services.AddSingleton<IExportService, ExportService>();
		builder.Services.AddSingleton<IImportService, ImportService>();
#if ANDROID
		builder.Services.AddSingleton<IDeviceFileSaveService, DeviceFileSaveService>();
#endif

		// ViewModels
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<SetupViewModel>();
		builder.Services.AddTransient<SessionListViewModel>();
		builder.Services.AddTransient<SessionDetailViewModel>();
		builder.Services.AddTransient<RoomViewModel>();
		builder.Services.AddTransient<ScannerViewModel>();
		builder.Services.AddTransient<ScanHistoryViewModel>();
		builder.Services.AddTransient<ExportImportViewModel>();
		builder.Services.AddTransient<DashboardViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();

		// Views (Pages)
		builder.Services.AddSingleton<AppShell>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<SetupPage>();
		builder.Services.AddTransient<SessionListPage>();
		builder.Services.AddTransient<SessionDetailPage>();
		builder.Services.AddTransient<ScannerPage>();
		builder.Services.AddTransient<ScannerV2Page>();
		builder.Services.AddTransient<ScanHistoryPage>();
		builder.Services.AddTransient<ExportImportPage>();
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<SettingsPage>();

		return builder.Build();
	}
}
