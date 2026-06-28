namespace InventoryScanner.Views;

public partial class ExportImportPage : ContentPage
{
    private readonly ViewModels.ExportImportViewModel _viewModel;

    public ExportImportPage(ViewModels.ExportImportViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _viewModel.LoadSessionsCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExportImportPage OnAppearing failed: {ex}");
        }
    }
}
