using InventoryScanner.ViewModels;

#if ANDROID
#pragma warning disable CS0618
using Android.Graphics;
using Android.Hardware;
using Android.Views;
using Microsoft.Maui.Platform;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Barcode.Common;
using Xamarin.Google.MLKit.Vision.Common;
using ACamera = Android.Hardware.Camera;
#endif

namespace InventoryScanner.Views;

public partial class ScannerV2Page : ContentPage
{
    private readonly ScannerViewModel _viewModel;

#if ANDROID
    private NativeMlKitCameraController? _cameraController;
    private SurfaceView? _previewView;
    private bool _pageAppeared;
    private bool _cameraPermissionGranted;
#endif

    public ScannerV2Page(ScannerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await _viewModel.LoadSessionInfoCommand.ExecuteAsync(null);

#if ANDROID
            var permission = await Permissions.RequestAsync<Permissions.Camera>();
            if (permission != PermissionStatus.Granted)
            {
                await DisplayAlertAsync("Camera Permission", "Camera permission is required for Scan (V2).", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            _cameraPermissionGranted = true;
            _pageAppeared = true;
            TryStartAndroidCamera();
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ScannerV2Page OnAppearing failed: {ex}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

#if ANDROID
        _pageAppeared = false;
        StopAndroidCamera();
#endif
    }

#if ANDROID
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        TryStartAndroidCamera();
    }

    private void TryStartAndroidCamera()
    {
        if (!_pageAppeared || !_cameraPermissionGranted || _cameraController != null)
            return;

        if (CameraPreviewHost.Handler?.PlatformView is not ViewGroup previewHost)
            return;

        if (previewHost.Width <= 0 || previewHost.Height <= 0)
        {
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(100), TryStartAndroidCamera);
            return;
        }

        var activity = Platform.CurrentActivity;
        if (activity == null)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
                await DisplayAlertAsync("Scan (V2)", "Unable to start the Google ML Kit scanner.\n\nNo Android activity is available.", "OK"));
            return;
        }

        var previewView = new SurfaceView(activity)
        {
            LayoutParameters = new ViewGroup.LayoutParams(previewHost.Width, previewHost.Height)
        };

        previewView.SetMinimumWidth(previewHost.Width);
        previewView.SetMinimumHeight(previewHost.Height);

        previewHost.RemoveAllViews();
        previewHost.AddView(previewView);
        previewView.Measure(
            Android.Views.View.MeasureSpec.MakeMeasureSpec(previewHost.Width, MeasureSpecMode.Exactly),
            Android.Views.View.MeasureSpec.MakeMeasureSpec(previewHost.Height, MeasureSpecMode.Exactly));
        previewView.Layout(0, 0, previewHost.Width, previewHost.Height);
        previewView.RequestLayout();

        _previewView = previewView;
        _cameraController = new NativeMlKitCameraController(previewView, previewHost, _viewModel);
        _cameraController.Start();
    }

    private void StopAndroidCamera()
    {
        _cameraController?.Dispose();
        _cameraController = null;

        if (_previewView?.Parent is ViewGroup parent)
            parent.RemoveView(_previewView);

        _previewView = null;
    }

    private sealed class NativeMlKitCameraController : Java.Lang.Object, ISurfaceHolderCallback, ACamera.IPreviewCallback, IDisposable
    {
        private static readonly TimeSpan DetectionCooldown = TimeSpan.FromMilliseconds(1600);

        private readonly SurfaceView _previewView;
        private readonly ViewGroup _previewHost;
        private readonly ScannerViewModel _viewModel;
        private readonly IBarcodeScanner _scanner;
        private ACamera? _camera;
        private int _cameraId = -1;
        private int _previewWidth;
        private int _previewHeight;
        private bool _disposed;
        private bool _isProcessing;
        private string _lastValue = string.Empty;
        private DateTime _lastDetectedAt = DateTime.MinValue;

        public NativeMlKitCameraController(SurfaceView previewView, ViewGroup previewHost, ScannerViewModel viewModel)
        {
            _previewView = previewView;
            _previewHost = previewHost;
            _viewModel = viewModel;

            var options = new BarcodeScannerOptions.Builder()
                .SetBarcodeFormats(Barcode.FormatAllFormats)
                .Build();
            _scanner = BarcodeScanning.GetClient(options);
        }

        public void Start()
        {
            try
            {
                _previewView.Holder?.AddCallback(this);

                _previewView.Post(() =>
                {
                    if (_disposed)
                        return;

                    EnsurePreviewSize();
                    var holder = _previewView.Holder;
                    if (holder?.Surface?.IsValid == true)
                        OpenCamera(holder);
                });
            }
            catch (Exception ex)
            {
                ShowStartupError(ex);
            }
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            EnsurePreviewSize();
            OpenCamera(holder);
        }

        public void SurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
        {
            if (_disposed)
                return;

            EnsurePreviewSize();
            if (_camera != null)
                RestartPreview(holder);
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            StopCamera();
        }

        public void OnPreviewFrame(byte[]? data, ACamera? camera)
        {
            if (_disposed || data == null || camera == null)
                return;

            if (!_viewModel.IsScanning || _isProcessing)
            {
                camera.AddCallbackBuffer(data);
                return;
            }

            _isProcessing = true;

            try
            {
                var rotations = BuildRotationCandidates(GetMlKitRotationDegrees(_cameraId));
                ProcessFrame(data, rotations, 0);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Google ML Kit frame processing failed: {ex}");
                ReleaseFrame(data);
            }
        }

        private void ProcessFrame(byte[] data, IReadOnlyList<int> rotations, int rotationIndex)
        {
            if (_disposed)
            {
                ReleaseFrame(data);
                return;
            }

            if (rotationIndex >= rotations.Count)
            {
                ReleaseFrame(data);
                return;
            }

            try
            {
                var image = InputImage.FromByteArray(
                    data,
                    _previewWidth,
                    _previewHeight,
                    rotations[rotationIndex],
                    InputImage.ImageFormatNv21);

                _scanner.Process(image)
                    .AddOnSuccessListener(new BarcodeSuccessListener(this, data, rotations, rotationIndex))
                    .AddOnFailureListener(new BarcodeFailureListener(this, data, rotations, rotationIndex));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Google ML Kit frame processing failed at rotation {rotations[rotationIndex]}: {ex}");
                ProcessFrame(data, rotations, rotationIndex + 1);
            }
        }

        private void OpenCamera(ISurfaceHolder holder)
        {
            if (_disposed || _camera != null)
                return;

            try
            {
                _cameraId = GetRearCameraId();
                _camera = _cameraId >= 0 ? ACamera.Open(_cameraId) : ACamera.Open();
                if (_camera == null)
                    throw new InvalidOperationException("Android camera service did not return a camera instance.");

                ConfigureCamera();
                _camera.SetDisplayOrientation(GetPreviewDisplayOrientation(_cameraId));
                _camera.SetPreviewDisplay(holder);
                StartPreview();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Google ML Kit native camera startup failed: {ex}");
                StopCamera();
                ShowStartupError(ex);
            }
        }

        private void ConfigureCamera()
        {
            if (_camera == null)
                return;

            var parameters = _camera.GetParameters();
            if (parameters?.SupportedPreviewSizes == null || parameters.SupportedPreviewSizes.Count == 0)
                throw new InvalidOperationException("The camera does not expose any supported preview sizes.");

            var size = SelectPreviewSize(parameters.SupportedPreviewSizes);
            _previewWidth = size.Width;
            _previewHeight = size.Height;
            System.Diagnostics.Debug.WriteLine($"Google ML Kit scanner preview size: {_previewWidth}x{_previewHeight}");

            parameters.SetPreviewSize(_previewWidth, _previewHeight);
            parameters.PreviewFormat = ImageFormatType.Nv21;

            if (parameters.SupportedFocusModes?.Contains(ACamera.Parameters.FocusModeContinuousPicture) == true)
                parameters.FocusMode = ACamera.Parameters.FocusModeContinuousPicture;
            else if (parameters.SupportedFocusModes?.Contains(ACamera.Parameters.FocusModeContinuousVideo) == true)
                parameters.FocusMode = ACamera.Parameters.FocusModeContinuousVideo;
            else if (parameters.SupportedFocusModes?.Contains(ACamera.Parameters.FocusModeAuto) == true)
                parameters.FocusMode = ACamera.Parameters.FocusModeAuto;

            if (parameters.SupportedSceneModes?.Contains(ACamera.Parameters.SceneModeBarcode) == true)
                parameters.SceneMode = ACamera.Parameters.SceneModeBarcode;

            if (parameters.IsVideoStabilizationSupported)
                parameters.VideoStabilization = true;

            parameters.AutoExposureLock = false;
            parameters.AutoWhiteBalanceLock = false;

            _camera.SetParameters(parameters);
        }

        private void StartPreview()
        {
            if (_camera == null)
                return;

            var bitsPerPixel = Android.Graphics.ImageFormat.GetBitsPerPixel(ImageFormatType.Nv21);
            var bufferSize = _previewWidth * _previewHeight * bitsPerPixel / 8;
            _camera.AddCallbackBuffer(new byte[bufferSize]);
            _camera.AddCallbackBuffer(new byte[bufferSize]);
            _camera.SetPreviewCallbackWithBuffer(this);
            _camera.StartPreview();
        }

        private void RestartPreview(ISurfaceHolder holder)
        {
            try
            {
                _camera?.StopPreview();
                _camera?.SetPreviewDisplay(holder);
                _camera?.StartPreview();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Google ML Kit native camera preview restart failed: {ex}");
            }
        }

        private void EnsurePreviewSize()
        {
            var width = _previewHost.Width;
            var height = _previewHost.Height;
            if (width <= 0 || height <= 0)
                return;

            if (_previewView.Width > 0 && _previewView.Height > 0)
                return;

            _previewView.LayoutParameters = new ViewGroup.LayoutParams(width, height);
            _previewView.Measure(
                Android.Views.View.MeasureSpec.MakeMeasureSpec(width, MeasureSpecMode.Exactly),
                Android.Views.View.MeasureSpec.MakeMeasureSpec(height, MeasureSpecMode.Exactly));
            _previewView.Layout(0, 0, width, height);
            _previewView.RequestLayout();
        }

        private bool TryHandleBarcodes(IList<Barcode> barcodes)
        {
            var barcode = barcodes.FirstOrDefault(b => !string.IsNullOrWhiteSpace(b.RawValue));
            if (barcode == null)
                return false;

            var value = barcode.RawValue!;
            var now = DateTime.UtcNow;
            if (value == _lastValue && now - _lastDetectedAt < DetectionCooldown)
                return true;

            _lastValue = value;
            _lastDetectedAt = now;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                _viewModel.LastBarcodeType = barcode.Format.ToString();
                await _viewModel.ProcessBarcodeCommand.ExecuteAsync(value);
            });

            return true;
        }

        private void ReleaseFrame(byte[]? frame)
        {
            _isProcessing = false;

            if (!_disposed && frame != null)
                _camera?.AddCallbackBuffer(frame);
        }

        private void StopCamera()
        {
            try
            {
                _camera?.SetPreviewCallbackWithBuffer(null);
                _camera?.StopPreview();
                _camera?.Release();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Google ML Kit native camera cleanup failed: {ex}");
            }
            finally
            {
                _camera = null;
            }
        }

        private static ACamera.Size SelectPreviewSize(IList<ACamera.Size> sizes)
        {
            const double targetRatio = 4d / 3d;
            const int targetPixels = 1280 * 960;
            return sizes
                .Where(s => s.Width * s.Height <= 1920 * 1080)
                .OrderBy(s => Math.Abs((s.Width / (double)s.Height) - targetRatio))
                .ThenBy(s => Math.Abs((s.Width * s.Height) - targetPixels))
                .ThenByDescending(s => s.Width * s.Height)
                .DefaultIfEmpty(sizes.OrderByDescending(s => s.Width * s.Height).First())
                .First();
        }

        private static int GetRearCameraId()
        {
            var cameraInfo = new ACamera.CameraInfo();
            for (var i = 0; i < ACamera.NumberOfCameras; i++)
            {
                ACamera.GetCameraInfo(i, cameraInfo);
                if (cameraInfo.Facing == CameraFacing.Back)
                    return i;
            }

            return -1;
        }

        private static int GetPreviewDisplayOrientation(int cameraId)
        {
            if (cameraId < 0 || Platform.CurrentActivity?.WindowManager?.DefaultDisplay == null)
                return 90;

            var cameraInfo = new ACamera.CameraInfo();
            ACamera.GetCameraInfo(cameraId, cameraInfo);

            var degrees = Platform.CurrentActivity.WindowManager.DefaultDisplay.Rotation switch
            {
                SurfaceOrientation.Rotation90 => 90,
                SurfaceOrientation.Rotation180 => 180,
                SurfaceOrientation.Rotation270 => 270,
                _ => 0
            };

            return cameraInfo.Facing == CameraFacing.Front
                ? (360 - ((cameraInfo.Orientation + degrees) % 360)) % 360
                : (cameraInfo.Orientation - degrees + 360) % 360;
        }

        private static int GetMlKitRotationDegrees(int cameraId)
        {
            if (cameraId < 0 || Platform.CurrentActivity?.WindowManager?.DefaultDisplay == null)
                return 90;

            var cameraInfo = new ACamera.CameraInfo();
            ACamera.GetCameraInfo(cameraId, cameraInfo);

            var deviceRotation = Platform.CurrentActivity.WindowManager.DefaultDisplay.Rotation switch
            {
                SurfaceOrientation.Rotation90 => 90,
                SurfaceOrientation.Rotation180 => 180,
                SurfaceOrientation.Rotation270 => 270,
                _ => 0
            };

            return cameraInfo.Facing == CameraFacing.Front
                ? (cameraInfo.Orientation + deviceRotation) % 360
                : (cameraInfo.Orientation - deviceRotation + 360) % 360;
        }

        private static IReadOnlyList<int> BuildRotationCandidates(int primaryRotation)
        {
            var candidates = new[] { primaryRotation, 90, 0, 180, 270 };
            return candidates.Distinct().ToArray();
        }

        private static void ShowStartupError(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Google ML Kit native scanner startup failed: {ex}");
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var message = $"Unable to start the Google ML Kit scanner.\n\n{ex.GetType().Name}: {ex.Message}";
                await Shell.Current.CurrentPage.DisplayAlertAsync("Scan (V2)", message, "OK");
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;
            StopCamera();
            _scanner.Close();
            _previewView.Holder?.RemoveCallback(this);
            base.Dispose(disposing);
        }

        private sealed class BarcodeSuccessListener : Java.Lang.Object, Android.Gms.Tasks.IOnSuccessListener
        {
            private readonly NativeMlKitCameraController _owner;
            private readonly byte[] _frame;
            private readonly IReadOnlyList<int> _rotations;
            private readonly int _rotationIndex;

            public BarcodeSuccessListener(NativeMlKitCameraController owner, byte[] frame, IReadOnlyList<int> rotations, int rotationIndex)
            {
                _owner = owner;
                _frame = frame;
                _rotations = rotations;
                _rotationIndex = rotationIndex;
            }

            public void OnSuccess(Java.Lang.Object? result)
            {
                if (result is Java.Util.IList javaList)
                {
                    var barcodes = new List<Barcode>();
                    for (var i = 0; i < javaList.Size(); i++)
                    {
                        if (javaList.Get(i) is Barcode barcode)
                            barcodes.Add(barcode);
                    }

                    if (_owner.TryHandleBarcodes(barcodes))
                    {
                        _owner.ReleaseFrame(_frame);
                        return;
                    }

                    _owner.ProcessFrame(_frame, _rotations, _rotationIndex + 1);
                    return;
                }

                _owner.ProcessFrame(_frame, _rotations, _rotationIndex + 1);
            }
        }

        private sealed class BarcodeFailureListener : Java.Lang.Object, Android.Gms.Tasks.IOnFailureListener
        {
            private readonly NativeMlKitCameraController _owner;
            private readonly byte[] _frame;
            private readonly IReadOnlyList<int> _rotations;
            private readonly int _rotationIndex;

            public BarcodeFailureListener(NativeMlKitCameraController owner, byte[] frame, IReadOnlyList<int> rotations, int rotationIndex)
            {
                _owner = owner;
                _frame = frame;
                _rotations = rotations;
                _rotationIndex = rotationIndex;
            }

            public void OnFailure(Java.Lang.Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Google ML Kit barcode scan failed: {e}");
                _owner.ProcessFrame(_frame, _rotations, _rotationIndex + 1);
            }
        }
    }
#pragma warning restore CS0618
#endif
}
