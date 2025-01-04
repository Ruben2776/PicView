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
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using ReactiveUI;

namespace PicView.Avalonia.Views;

public partial class SingleImageResizeView : UserControl
{
    private double _aspectRatio;
    private IDisposable? _imageUpdateSubscription;
    private bool _isKeepingAspectRatio = true;

    public SingleImageResizeView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        _aspectRatio = (double)vm.PixelWidth / vm.PixelHeight;
        InitializeEventHandlers(vm);
        _imageUpdateSubscription = vm.WhenAnyValue(x => x.FileInfo).Select(x => x is not null).Subscribe(_ =>
        {
            Dispatcher.UIThread.Invoke(SetIsQualitySliderEnabled);
        });
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        _imageUpdateSubscription?.Dispose();
    }

    private void InitializeEventHandlers(MainViewModel vm)
    {
        SetIsQualitySliderEnabled();
        SaveButton.Click += async (_, _) => await SaveImage(vm).ConfigureAwait(false);
        SaveAsButton.Click += async (_, _) => await SaveImageAs(vm).ConfigureAwait(false);

        PixelWidthTextBox.KeyDown += async (_, e) => await SaveImageOnEnter(e, vm);
        PixelHeightTextBox.KeyDown += async (_, e) => await SaveImageOnEnter(e, vm);

        PixelWidthTextBox.KeyUp += delegate { AdjustAspectRatio(PixelWidthTextBox); };
        PixelHeightTextBox.KeyUp += delegate { AdjustAspectRatio(PixelHeightTextBox); };

        ConversionComboBox.SelectionChanged += delegate { SetIsQualitySliderEnabled(); };

        ResetButton.Click += (_, _) => ResetSettings(vm);

        LinkChainButton.Click += (_, _) => ToggleAspectRatio();
    }

    private void AdjustAspectRatio(TextBox sender)
    {
        if (!_isKeepingAspectRatio) return;

        AspectRatioHelper.SetAspectRatioForTextBox(PixelWidthTextBox, PixelHeightTextBox, sender == PixelWidthTextBox,
            _aspectRatio, DataContext as MainViewModel);
    }

    private void SetIsQualitySliderEnabled()
    {
        if (DataContext is not MainViewModel vm) return;

        try
        {
            if (JpgItem.IsSelected || PngItem.IsSelected)
            {
                QualitySlider.IsEnabled = true;
                QualitySlider.Value = 75;
            }
            else if (vm.FileInfo.Extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                     vm.FileInfo.Extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                     vm.FileInfo.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
            {
                QualitySlider.IsEnabled = true;
                var quality = ImageFunctionHelper.GetCompressionQuality(vm.FileInfo.FullName);
                QualitySlider.Value = quality;
            }
            else
            {
                QualitySlider.IsEnabled = false;
            }
        }
        catch (Exception e)
        {
#if DEBUG
            Console.WriteLine(e);
#endif
        }
    }

    private async Task SaveImageOnEnter(KeyEventArgs e, MainViewModel vm)
    {
        if (e.Key == Key.Enter)
        {
            await SaveImage(vm).ConfigureAwait(false);
        }
    }

    private async Task SaveImageAs(MainViewModel vm)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
        {
            return;
        }

        var fileInfoFullName = vm.FileInfo.FullName;
        var ext = DetermineFileExtension(vm, ref fileInfoFullName);
        
        var suggestedFileName = Path.ChangeExtension(vm.FileInfo.Name, ext);

        var options = new FilePickerSaveOptions
        {
            Title = $"{TranslationHelper.Translation.OpenFileDialog} - PicView",
            SuggestedFileName = suggestedFileName,
            SuggestedStartLocation =
                await desktop.MainWindow.StorageProvider.TryGetFolderFromPathAsync(fileInfoFullName),
        };
        var file = await provider.SaveFilePickerAsync(options);
        if (file is null) return;

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
        
        await Dispatcher.UIThread.InvokeAsync(() => SetLoadingState(true));

        const int rotationAngle = 0; // TODO make a control for adjusting rotation

        var file = vm.FileInfo.FullName;
        var ext = DetermineFileExtension(vm, ref destination);
        destination = Path.ChangeExtension(destination, ext);
        var sameFile = file.Equals(destination, StringComparison.OrdinalIgnoreCase);


        var quality = GetQualityValue(ext, destination);

        var success = await SaveImageFileHelper.SaveImageAsync(null,
            file,
            sameFile ? null : destination,
            width,
            height,
            quality,
            ext,
            rotationAngle,
            null,
            _isKeepingAspectRatio).ConfigureAwait(false);

        await Dispatcher.UIThread.InvokeAsync(() => SetLoadingState(false));

        if (!success)
        {
            await TooltipHelper.ShowTooltipMessageAsync(TranslationHelper.Translation.SavingFileFailed);
            return;
        }

        await HandlePostSaveActions(vm, file, destination);
        if (Path.GetExtension(file) != ext)
        {
            FileDeletionHelper.DeleteFileWithErrorMsg(file, true);
        }
    }

    private void SetLoadingState(bool isLoading)
    {
        ParentContainer.Opacity = isLoading ? .1 : 1;
        ParentContainer.IsHitTestVisible = !isLoading;
        SpinWaiter.IsVisible = isLoading;
    }

    private string DetermineFileExtension(MainViewModel vm, ref string destination)
    {
        var ext = vm.FileInfo.Extension;
        if (NoConversion.IsSelected)
        {
            return ext;
        }

        if (PngItem.IsSelected)
        {
            ext = ".png";
        }
        else if (JpgItem.IsSelected)
        {
            ext = ".jpg";
        }
        else if (WebpItem.IsSelected)
        {
            ext = ".webp";
        }
        else if (AvifItem.IsSelected)
        {
            ext = ".avif";
        }
        else if (HeicItem.IsSelected)
        {
            ext = ".heic";
        }
        else if (JxlItem.IsSelected)
        {
            ext = ".jxl";
        }
        destination = Path.ChangeExtension(destination, ext);
        return ext;
    }

    private uint? GetQualityValue(string ext, string destination)
    {
        if (QualitySlider.IsEnabled && (ext == ".jpg" || Path.GetExtension(destination) == ".jpg" || Path.GetExtension(destination) == ".jpeg"))
        {
            return (uint)QualitySlider.Value;
        }
        return null;
    }

    private static async Task HandlePostSaveActions(MainViewModel vm, string file, string destination)
    {
        if (destination == file)
        {
            if (NavigationHelper.CanNavigate(vm) && vm.ImageIterator is not null)
            {
                await vm.ImageIterator.QuickReload().ConfigureAwait(false);
            }
        }
        else if (Path.GetDirectoryName(file) == Path.GetDirectoryName(destination))
        {
            await NavigationHelper.LoadPicFromFile(destination, vm).ConfigureAwait(false);
        }
    }

    private void ResetSettings(MainViewModel vm)
    {
        PixelWidthTextBox.Text = vm.PixelWidth.ToString();
        PixelHeightTextBox.Text = vm.PixelHeight.ToString();
        if (vm.FileInfo.Extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
            vm.FileInfo.Extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
            vm.FileInfo.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            QualitySlider.IsEnabled = true;
            var quality = ImageFunctionHelper.GetCompressionQuality(vm.FileInfo.FullName);
            QualitySlider.Value = quality;
        }
        else
        {
            QualitySlider.IsEnabled = false;
        }
        ConversionComboBox.SelectedItem = NoConversion;
    }

    private void ToggleAspectRatio()
    {
        _isKeepingAspectRatio = !_isKeepingAspectRatio;
        LinkChainImage.IsVisible = _isKeepingAspectRatio;
        UnlinkChainImage.IsVisible = !_isKeepingAspectRatio;

        if (_isKeepingAspectRatio)
        {
            AdjustAspectRatio(PixelWidthTextBox);
        }
    }

    ~SingleImageResizeView()
    {
        _imageUpdateSubscription?.Dispose();
    }
}