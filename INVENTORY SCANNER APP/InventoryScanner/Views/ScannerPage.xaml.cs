using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace InventoryScanner.Views;

public partial class ScannerPage : ContentPage
{
    private static readonly TimeSpan DuplicateDetectionCooldown = TimeSpan.FromMilliseconds(1200);
    private const int TargetAnalysisPixels = 1280 * 720;

    private readonly ViewModels.ScannerViewModel _viewModel;
    private CameraBarcodeReaderView? _barcodeReader;
    private Grid? _cameraHost;
    private int _cameraPlaceholderIndex;
    private bool _isTorchOn;
    private bool _isTorchSupported = true;
    private bool _isProcessingDetection;
    private DateTime _lastDetectedAt = DateTime.MinValue;
    private string _lastDetectedValue = string.Empty;

    public ScannerPage(ViewModels.ScannerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        SetTorchButtonState(false);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            InitializeBarcodeReader();
            _lastDetectedValue = string.Empty;
            _lastDetectedAt = DateTime.MinValue;
            _isProcessingDetection = false;

            await _viewModel.LoadSessionInfoCommand.ExecuteAsync(null);

            if (_barcodeReader != null)
                _barcodeReader.IsDetecting = _viewModel.IsScanning;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ScannerPage OnAppearing failed: {ex}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        DisposeBarcodeReader();
    }

    private void InitializeBarcodeReader()
    {
        if (_barcodeReader != null) return;

        _barcodeReader = new CameraBarcodeReaderView
        {
            Options = new BarcodeReaderOptions
            {
                Formats = BarcodeFormats.TwoDimensional,
                AutoRotate = true,
                Multiple = false,
                TryHarder = true,
                TryInverted = true,
                CameraResolutionSelector = SelectScannerResolution
            },
            IsTorchOn = false,
            CameraLocation = CameraLocation.Rear,
            IsDetecting = _viewModel.IsScanning
        };

        _barcodeReader.BarcodesDetected += OnBarcodesDetected;

        _cameraHost = CameraPlaceholder.Parent as Grid;
        if (_cameraHost != null)
        {
            _cameraPlaceholderIndex = _cameraHost.Children.IndexOf(CameraPlaceholder);
            _cameraHost.Children.Remove(CameraPlaceholder);
            _cameraHost.Children.Insert(_cameraPlaceholderIndex, _barcodeReader);
        }
    }

    private static CameraResolution SelectScannerResolution(IReadOnlyList<CameraResolution> resolutions)
    {
        var selected = resolutions
            .Where(resolution => resolution.Width > 0 && resolution.Height > 0)
            .Where(resolution => resolution.Width * resolution.Height <= 1920 * 1080)
            .OrderBy(resolution => Math.Abs((resolution.Width * resolution.Height) - TargetAnalysisPixels))
            .ThenByDescending(resolution => resolution.Width * resolution.Height)
            .FirstOrDefault();

        return selected ?? resolutions.First();
    }

    private void DisposeBarcodeReader()
    {
        if (_barcodeReader == null)
            return;

        try
        {
            _barcodeReader.IsDetecting = false;
            TrySetTorch(false);
            _barcodeReader.BarcodesDetected -= OnBarcodesDetected;

            if (_cameraHost != null)
            {
                _cameraHost.Children.Remove(_barcodeReader);
                if (!CameraPlaceholder.IsLoaded && !(_cameraHost.Children.Contains(CameraPlaceholder)))
                    _cameraHost.Children.Insert(Math.Min(_cameraPlaceholderIndex, _cameraHost.Children.Count), CameraPlaceholder);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ScannerPage camera cleanup failed: {ex}");
        }
        finally
        {
            _barcodeReader = null;
            SetTorchButtonState(false);
        }
    }

    private void OnTorchButtonClicked(object? sender, EventArgs e)
    {
        if (_barcodeReader == null || !_isTorchSupported)
            return;

        TrySetTorch(!_isTorchOn);
    }

    private void TrySetTorch(bool isTorchOn)
    {
        if (_barcodeReader == null)
            return;

        try
        {
            _barcodeReader.IsTorchOn = isTorchOn;
            SetTorchButtonState(isTorchOn);
        }
        catch (Exception ex)
        {
            _isTorchSupported = false;
            _barcodeReader.IsTorchOn = false;
            SetTorchButtonUnavailable();
            System.Diagnostics.Debug.WriteLine($"Torch is not available on this device: {ex}");
        }
    }

    private void SetTorchButtonState(bool isTorchOn)
    {
        _isTorchOn = isTorchOn;
        TorchButton.BackgroundColor = isTorchOn ? Color.FromArgb("#0F766E") : Color.FromArgb("#1E293B");
        TorchButton.Source = ImageSource.FromFile(isTorchOn ? "torch_on.png" : "torch_off.png");
    }

    private void SetTorchButtonUnavailable()
    {
        _isTorchOn = false;
        TorchButton.IsEnabled = false;
        TorchButton.Opacity = 0.35;
        TorchButton.BackgroundColor = Color.FromArgb("#1E293B");
        TorchButton.Source = ImageSource.FromFile("torch_off.png");
    }

    private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        var result = e.Results?.FirstOrDefault();
        if (result == null || !_viewModel.IsScanning) return;

        var now = DateTime.UtcNow;
        if (_isProcessingDetection ||
            (result.Value == _lastDetectedValue && now - _lastDetectedAt < DuplicateDetectionCooldown))
        {
            return;
        }

        _isProcessingDetection = true;
        _lastDetectedValue = result.Value;
        _lastDetectedAt = now;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                if (_barcodeReader != null)
                    _barcodeReader.IsDetecting = false;

                _viewModel.LastBarcodeType = result.Format.ToString();
                await _viewModel.ProcessBarcodeCommand.ExecuteAsync(result.Value);

                await Task.Delay(DuplicateDetectionCooldown);
            }
            finally
            {
                _isProcessingDetection = false;

                if (_barcodeReader != null && _viewModel.IsScanning)
                    _barcodeReader.IsDetecting = true;
            }
        });
    }
}
