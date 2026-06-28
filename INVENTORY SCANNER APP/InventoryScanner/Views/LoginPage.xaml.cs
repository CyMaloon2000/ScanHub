namespace InventoryScanner.Views;

public partial class LoginPage : ContentPage
{
    private readonly ViewModels.LoginViewModel _viewModel;

    public LoginPage(ViewModels.LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            // Small delay to let Shell fully initialize on Android
            await Task.Delay(200);
            await _viewModel.CheckAuthCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CheckAuth on appearing failed: {ex}");
        }
    }
}
