namespace InventoryScanner.Views;

public partial class SessionDetailPage : ContentPage
{
    private readonly ViewModels.SessionDetailViewModel _viewModel;

    public SessionDetailPage(ViewModels.SessionDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _viewModel.LoadSessionCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SessionDetailPage OnAppearing failed: {ex}");
        }
    }
}
