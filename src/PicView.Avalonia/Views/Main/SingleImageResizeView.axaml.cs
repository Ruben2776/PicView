using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using PicView.Avalonia.FileSystem;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Resizing;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.DebugTools;
using PicView.Core.ImageDecoding;
using R3;

namespace PicView.Avalonia.Views.Main;

public partial class SingleImageResizeView : UserControl
{
    private double _aspectRatio;
    private readonly CompositeDisposable _imageUpdateSubscription = new();
    private bool _isKeepingAspectRatio = true;

    public SingleImageResizeView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        
        if (!Settings.Theme.Dark || Settings.Theme.GlassTheme)
        {
            BgPanel.Background = Brushes.Transparent;
        }
        if (!Settings.Theme.Dark)
        {
            var topBg = new SolidColorBrush(Color.FromArgb(a: 65, r: 162, g: 162, b: 162));
            var bottomBg = new SolidColorBrush(Color.FromArgb(a: 93, r: 162, g: 162, b: 162));
            MainBorder.Background = topBg;
            BottomBorder.Background = bottomBg;

            var noThickness = new Thickness(0);
            PixelWidthTextBox.BorderThickness = noThickness;
            PixelHeightTextBox.BorderThickness = noThickness;
            
            if (TryGetResource("CancelBrush",
                    Application.Current.RequestedThemeVariant, out var cBrush))
            {
                if (cBrush is SolidColorBrush brush)
                {
                    UIHelper.SetButtonHover(CancelButton, brush);
                }
            }
            UIHelper.SwitchAccentHoverClass(CancelButton);
        }

        _aspectRatio = (double)vm.PicViewer.PixelWidth.CurrentValue / vm.PicViewer.PixelHeight.CurrentValue;

        RegisterEventHandlers(vm);

        Observable.EveryValueChanged(vm.PicViewer, x => x.FileInfo.Value, UIHelper.GetFrameProvider)
            .Subscribe(_ =>
            {
                UpdateQualitySliderState();
                ShowCancelButton();
            }).AddTo(_imageUpdateSubscription);
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        _imageUpdateSubscription?.Dispose();
    }

    private void RegisterEventHandlers(MainViewModel vm)
    {
        UpdateQualitySliderState();
        QualitySlider.ValueChanged += (_, _) => ShowResetButton();

        SaveButton.Click += async (_, _) => await SaveImage(vm).ConfigureAwait(false);
        SaveAsButton.Click += async (_, _) => await SaveImageAs(vm).ConfigureAwait(false);

        PixelWidthTextBox.KeyDown += async (_, e) => await SaveImageOnEnter(e, vm);
        PixelHeightTextBox.KeyDown += async (_, e) => await SaveImageOnEnter(e, vm);

        PixelWidthTextBox.KeyUp += (_, _) => AdjustAspectRatio(PixelWidthTextBox);
        PixelHeightTextBox.KeyUp += (_, _) => AdjustAspectRatio(PixelHeightTextBox);

        ConversionComboBox.SelectionChanged += (_, _) =>
        {
            UpdateQualitySliderState();
            ShowResetButton();
        };

        ResetButton.Click += (_, _) => ResetSettings(vm);
        CancelButton.Click += (_, _) => (VisualRoot as Window)?.Close();

        LinkChainButton.Click += (_, _) => ToggleAspectRatio();
    }

    private void ShowResetButton()
    {
        CancelButton.IsVisible = false;
        ResetButton.IsVisible = true;
    }

    private void ShowCancelButton()
    {
        CancelButton.IsVisible = true;
        ResetButton.IsVisible = false;
    }

    private void AdjustAspectRatio(TextBox sender)
    {
        if (!_isKeepingAspectRatio)
        {
            return;
        }

        AspectRatioHelper.SetAspectRatioForTextBox(
            PixelWidthTextBox, PixelHeightTextBox, sender == PixelWidthTextBox,
            _aspectRatio, DataContext as MainViewModel);

        ShowResetButton();
    }

    private void UpdateQualitySliderState()
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        try
        {
            if (IsConversionToQualityFormat())
            {
                QualitySlider.IsEnabled = true;
                QualitySlider.Value = 75;
            }
            else if (IsOriginalFileQualityFormat(vm.PicViewer.FileInfo.CurrentValue.Extension))
            {
                QualitySlider.IsEnabled = true;
                QualitySlider.Value = ImageAnalyzer.GetCompressionQuality(vm.PicViewer.FileInfo.CurrentValue.FullName);
            }
            else
            {
                QualitySlider.IsEnabled = false;
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(SingleImageResizeView), nameof(UpdateQualitySliderState), e);
        }
    }

    private bool IsConversionToQualityFormat()
        => JpgItem.IsSelected || PngItem.IsSelected;

    private static bool IsOriginalFileQualityFormat(string ext)
        => ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
           || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
           || ext.Equals(".png", StringComparison.OrdinalIgnoreCase);

    private async Task SaveImageOnEnter(KeyEventArgs e, MainViewModel vm)
    {
        if (e.Key == Key.Enter)
        {
            await SaveImage(vm).ConfigureAwait(false);
        }
    }

    private async Task SaveImageAs(MainViewModel vm)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop
            || desktop.MainWindow?.StorageProvider is null)
        {
            return;
        }

        var fileInfoFullName = vm.PicViewer.FileInfo.CurrentValue.FullName;
        var ext = GetSelectedFileExtension(vm, ref fileInfoFullName);

        var file = await FilePicker.PickFileForSavingAsync(vm.PicViewer.FileInfo?.CurrentValue.FullName, ext);
        if (file is null)
        {
            return;
        }

        await DoSaveImage(vm, file).ConfigureAwait(false);
    }

    private async Task SaveImage(MainViewModel vm)
    {
        await DoSaveImage(vm, vm.PicViewer.FileInfo.CurrentValue.FullName).ConfigureAwait(false);
    }

    private async Task DoSaveImage(MainViewModel vm, string destination)
    {
        if (!uint.TryParse(PixelWidthTextBox.Text, out var width) ||
            !uint.TryParse(PixelHeightTextBox.Text, out var height))
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() => SetLoadingState(true));
        
        var ext = GetSelectedFileExtension(vm, ref destination);
        destination = Path.ChangeExtension(destination, ext);
        var sameFile = destination.Equals(vm.PicViewer.FileInfo.CurrentValue.FullName,
            StringComparison.OrdinalIgnoreCase);
        var quality = GetQualityValue(ext, destination);

        await SaveImageHandler.SaveImageWithPossibleNavigation(vm,
            vm.PicViewer.FileInfo.CurrentValue.FullName,
            destination,
            sameFile,
            width: width,
            height: height,
            quality: quality,
            ext: ext,
            isKeepingAspectRatio: _isKeepingAspectRatio);

        await Dispatcher.UIThread.InvokeAsync(() => SetLoadingState(false));
        
    }

    private void SetLoadingState(bool isLoading)
    {
        ParentContainer.Opacity = isLoading ? 0.1 : 1;
        ParentContainer.IsHitTestVisible = !isLoading;
        SpinWaiter.IsVisible = isLoading;
    }

    private string GetSelectedFileExtension(MainViewModel vm, ref string destination)
    {
        var ext = vm.PicViewer.FileInfo.CurrentValue.Extension;
        if (NoConversion.IsSelected)
        {
            return ext;
        }

        ext = GetExtensionFromSelectedItem() ?? ext;
        destination = Path.ChangeExtension(destination, ext);
        return ext;
    }

    private string? GetExtensionFromSelectedItem()
    {
        if (PngItem.IsSelected)
        {
            return ".png";
        }

        if (JpgItem.IsSelected)
        {
            return ".jpg";
        }

        if (WebpItem.IsSelected)
        {
            return ".webp";
        }

        if (AvifItem.IsSelected)
        {
            return ".avif";
        }

        if (HeicItem.IsSelected)
        {
            return ".heic";
        }

        if (JxlItem.IsSelected)
        {
            return ".jxl";
        }

        return null;
    }

    private uint? GetQualityValue(string ext, string destination)
    {
        if (QualitySlider.IsEnabled && (
                ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                Path.GetExtension(destination).Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                Path.GetExtension(destination).Equals(".jpeg", StringComparison.OrdinalIgnoreCase)))
        {
            return (uint)QualitySlider.Value;
        }

        return null;
    }

    private void ResetSettings(MainViewModel vm)
    {
        PixelWidthTextBox.Text = vm.PicViewer.PixelWidth.ToString();
        PixelHeightTextBox.Text = vm.PicViewer.PixelHeight.ToString();

        if (IsOriginalFileQualityFormat(vm.PicViewer.FileInfo.CurrentValue.Extension))
        {
            QualitySlider.IsEnabled = true;
            QualitySlider.Value = ImageAnalyzer.GetCompressionQuality(vm.PicViewer.FileInfo.CurrentValue.FullName);
        }
        else
        {
            QualitySlider.IsEnabled = false;
        }

        ConversionComboBox.SelectedItem = NoConversion;

        _isKeepingAspectRatio = true;
        ToggleLinkChain();

        ShowCancelButton();
    }

    private void ToggleAspectRatio()
    {
        _isKeepingAspectRatio = !_isKeepingAspectRatio;
        ToggleLinkChain();

        if (_isKeepingAspectRatio)
        {
            AdjustAspectRatio(PixelWidthTextBox);
        }

        if (!_isKeepingAspectRatio)
        {
            ShowResetButton();
        }
    }

    private void ToggleLinkChain()
    {
        if (!_isKeepingAspectRatio)
        {
            if (!Application.Current.TryGetResource("UnlinkChainImage",
                    Application.Current.RequestedThemeVariant, out var link))
            {
                return;
            }
            
            if (link is not DrawingImage linkImage)
            {
                return;
            }
            LinkChainButton.Icon  = linkImage;
        }
        else
        {
            if (!Application.Current.TryGetResource("LinkChainImage",
                    Application.Current.RequestedThemeVariant, out var link))
            {
                return;
            }
            
            if (link is not DrawingImage linkImage)
            {
                return;
            }
            LinkChainButton.Icon  = linkImage;
        }

    }

    ~SingleImageResizeView()
    {
        _imageUpdateSubscription?.Dispose();
    }
}