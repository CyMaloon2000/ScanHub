using InventoryScanner.ViewModels;

namespace InventoryScanner.Views
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage(SettingsViewModel viewModel)
        {
            try
            {
                InitializeComponent();
                BindingContext = viewModel;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SettingsPage initialization failed: {ex}");
                Title = "Settings";
                BindingContext = viewModel;
                Content = new ScrollView
                {
                    Padding = 20,
                    Content = new VerticalStackLayout
                    {
                        Spacing = 12,
                        Children =
                        {
                            new Label
                            {
                                Text = "Settings could not be loaded.",
                                FontSize = 20,
                                FontAttributes = FontAttributes.Bold,
                                TextColor = Colors.Red
                            },
                            new Label
                            {
                                Text = ex.Message,
                                FontSize = 13,
                                TextColor = Colors.Gray
                            }
                        }
                    }
                };
            }
        }
    }
}
