namespace InventoryScanner.Views;

public partial class SessionListPage : ContentPage
{
    private readonly ViewModels.SessionListViewModel _viewModel;

    public SessionListPage(ViewModels.SessionListViewModel viewModel)
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
            System.Diagnostics.Debug.WriteLine($"SessionListPage OnAppearing failed: {ex}");
        }
    }

    private void OnStatusFilterTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.BindingContext is string status)
        {
            _viewModel.SelectedStatusFilter = status;
            _ = _viewModel.SearchCommand.ExecuteAsync(null);
        }
    }
}
