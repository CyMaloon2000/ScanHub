namespace InventoryScanner.Views;

public partial class SetupPage : ContentPage
{
    private readonly ViewModels.SetupViewModel _viewModel;

    public SetupPage(ViewModels.SetupViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await _viewModel.LoadCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SetupPage OnAppearing failed: {ex}");
        }
    }
}
