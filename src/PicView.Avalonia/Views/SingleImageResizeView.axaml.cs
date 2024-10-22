using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Resizing;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using ReactiveUI;

namespace PicView.Avalonia.Views;

public partial class SingleImageResizeView : UserControl
{
    private double _aspectRatio;

    private IDisposable? _imageUpdateSubscription;

    // TODO: allow users to be able to disable aspect ratio locking if they want to stretch the image
    private readonly bool _isKeepingAspectRatio = true;

    public SingleImageResizeView()
    {
        InitializeComponent();
        Loaded += delegate
        {
            if (DataContext is not MainViewModel vm)
            {
                return;
            }

            _aspectRatio = (double)vm.PixelWidth / vm.PixelHeight;

            SetIsQualitySliderEnabled();
            SaveButton.Click += async (_, _) => await SaveImage(vm).ConfigureAwait(false);
            SaveAsButton.Click += async (_, _) => await SaveImageAs(vm).ConfigureAwait(false);

            PixelWidthTextBox.KeyDown += async (_, e) => await OnKeyDownVerifyInput(e);
            PixelHeightTextBox.KeyDown += async (_, e) => await OnKeyDownVerifyInput(e);

            PixelWidthTextBox.KeyUp += delegate { AdjustAspectRatio(PixelWidthTextBox); };
            PixelHeightTextBox.KeyUp += delegate { AdjustAspectRatio(PixelHeightTextBox); };

            ConversionComboBox.SelectionChanged += delegate { SetIsQualitySliderEnabled(); };

            _imageUpdateSubscription = vm.WhenAnyValue(x => x.FileInfo).Select(x => x is not null).Subscribe(_ =>
            {
                Dispatcher.UIThread.Post(SetIsQualitySliderEnabled);
            });
        };
    }

    private void AdjustAspectRatio(TextBox sender)
    {
        if (!_isKeepingAspectRatio)
        {
            return;
        }

        AspectRatioHelper.SetAspectRatioForTextBox(PixelWidthTextBox, PixelHeightTextBox, sender == PixelWidthTextBox,
            _aspectRatio, DataContext as MainViewModel);
    }

    private void SetIsQualitySliderEnabled()
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        if (JpgItem.IsSelected)
        {
            QualitySlider.IsEnabled = true;
        }
        else if (vm.FileInfo.Extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                 vm.FileInfo.Extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            QualitySlider.IsEnabled = true;
        }
        else
        {
            QualitySlider.IsEnabled = false;
        }
    }

    private async Task OnKeyDownVerifyInput(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.D0:
            case Key.D1:
            case Key.D2:
            case Key.D3:
            case Key.D4:
            case Key.D5:
            case Key.D6:
            case Key.D7:
            case Key.D8:
            case Key.D9:
            case Key.NumPad0:
            case Key.NumPad1:
            case Key.NumPad2:
            case Key.NumPad3:
            case Key.NumPad4:
            case Key.NumPad5:
            case Key.NumPad6:
            case Key.NumPad7:
            case Key.NumPad8:
            case Key.NumPad9:
            case Key.Back:
            case Key.Delete:
                break; // Allow numbers and basic operations

            case Key.Left:
            case Key.Right:
            case Key.Tab:
            case Key.OemBackTab:
                break; // Allow navigation keys

            case Key.A:
            case Key.C:
            case Key.X:
            case Key.V:
                if (e.KeyModifiers == KeyModifiers.Control)
                {
                    // Allow Ctrl + A, Ctrl + C, Ctrl + X, and Ctrl + V (paste)
                    break;
                }

                e.Handled = true; // Only allow with Ctrl
                return;

            case Key.Oem5: // Key for `%` symbol (may vary based on layout)
                break; // Allow the percentage symbol (%)

            case Key.Escape: // Handle Escape key
                Focus();
                e.Handled = true;
                return;

            case Key.Enter: // Handle Enter key for saving
                if (DataContext is not MainViewModel vm)
                {
                    return;
                }

                await SaveImage(vm).ConfigureAwait(false);
                return;

            default:
                e.Handled = true; // Block all other inputs
                return;
        }
    }

    private async Task SaveImageAs(MainViewModel vm)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
        {
            return;
        }

        var options = new FilePickerSaveOptions
        {
            Title = $"{TranslationHelper.Translation.OpenFileDialog} - PicView",
            SuggestedFileName = Path.GetFileNameWithoutExtension(vm.FileInfo.Name),
            SuggestedStartLocation =
                await desktop.MainWindow.StorageProvider.TryGetFolderFromPathAsync(vm.FileInfo.FullName)
        };
        var file = await provider.SaveFilePickerAsync(options);
        if (file is null)
        {
            return;
        }

        var destination = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? file.Path.AbsolutePath
            : file.Path.LocalPath;
        await DoSaveImage(vm, destination).ConfigureAwait(false);
    }

    private async Task SaveImage(MainViewModel vm)
    {
        await DoSaveImage(vm, vm.FileInfo.FullName).ConfigureAwait(false);
    }

    private async Task DoSaveImage(MainViewModel vm, string destination)
    {
        if (!uint.TryParse(PixelWidthTextBox.Text, out var width) ||
            !uint.TryParse(PixelHeightTextBox.Text, out var height))
        {
            return;
        }

        //Set loading and prevent user from interacting with UI
        ParentContainer.Opacity = .1;
        ParentContainer.IsHitTestVisible = false;
        SpinWaiter.IsVisible = true;

        var rotationAngle = 0; // TODO make a control for adjusting rotation

        var file = vm.FileInfo.FullName;
        var sameFile = file.Equals(destination, StringComparison.OrdinalIgnoreCase);
        var ext = vm.FileInfo.Extension;
        if (!NoConversion.IsSelected)
        {
            if (PngItem.IsSelected)
            {
                ext = ".png";
                destination = Path.ChangeExtension(destination, ".png");
            }
            else if (JpgItem.IsSelected)
            {
                ext = ".jpg";
                destination = Path.ChangeExtension(destination, ".jpg");
            }
            else if (WebpItem.IsSelected)
            {
                ext = ".webp";
                destination = Path.ChangeExtension(destination, ".webp");
            }
            else if (AvifItem.IsSelected)
            {
                ext = ".avif";
                destination = Path.ChangeExtension(destination, ".avif");
            }
            else if (HeicItem.IsSelected)
            {
                ext = ".heic";
                destination = Path.ChangeExtension(destination, ".heic");
            }
            else if (JxlItem.IsSelected)
            {
                ext = ".jxl";
                destination = Path.ChangeExtension(destination, ".jxl");
            }
        }

        uint? quality = null;
        if (QualitySlider.IsEnabled)
        {
            if (ext == ".jpg" || Path.GetExtension(destination) == ".jpg" || Path.GetExtension(destination) == ".jpeg")
            {
                quality = (uint)QualitySlider.Value;
            }
        }

        var success = await SaveImageFileHelper.SaveImageAsync(null,
            file,
            sameFile ? null : destination,
            width,
            height,
            quality,
            ext,
            rotationAngle).ConfigureAwait(false);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            SpinWaiter.IsVisible = false;
            ParentContainer.IsHitTestVisible = true;
            ParentContainer.Opacity = 1;
        });
        if (!success)
        {
            await TooltipHelper.ShowTooltipMessageAsync(TranslationHelper.Translation.SavingFileFailed);
            return;
        }

        if (destination == file)
        {
            if (!NavigationHelper.CanNavigate(vm))
            {
                return;
            }

            if (vm.ImageIterator is not null)
            {
                await vm.ImageIterator.QuickReload().ConfigureAwait(false);
            }
        }
        else
        {
            if (Path.GetDirectoryName(file) == Path.GetDirectoryName(destination))
            {
                await NavigationHelper.LoadPicFromFile(destination, vm).ConfigureAwait(false);
            }
        }
    }

    ~SingleImageResizeView()
    {
        _imageUpdateSubscription?.Dispose();
    }
}