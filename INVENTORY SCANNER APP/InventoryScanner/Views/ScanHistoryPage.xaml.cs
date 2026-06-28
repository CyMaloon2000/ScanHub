namespace InventoryScanner.Views;

public partial class ScanHistoryPage : ContentPage
{
    private readonly ViewModels.ScanHistoryViewModel _viewModel;

    public ScanHistoryPage(ViewModels.ScanHistoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _viewModel.LoadHistoryCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ScanHistoryPage OnAppearing failed: {ex}");
        }
    }
}
