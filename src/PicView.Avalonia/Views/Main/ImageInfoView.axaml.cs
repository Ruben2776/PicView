using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using PicView.Avalonia.Resizing;
using PicView.Avalonia.UI;
using PicView.Core.ViewModels;
using PicView.Core.Conversion;
using PicView.Core.Exif;
using PicView.Core.Extensions;
using PicView.Core.FileHandling;
using PicView.Core.Models;
using PicView.Core.Sizing;
using PicView.Core.Titles;
using R3;

namespace PicView.Avalonia.Views.Main;

public partial class ImageInfoView : UserControl
{
    private DisposableBag _disposables;

    public ImageInfoView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is not MainWindowViewModel vm)
            {
                return;
            }

            ResponsiveResizeUpdate(vm);

            KeyDown += (_, e) =>
            {
                switch (e.Key)
                {
                    case Key.Down:
                    case Key.PageDown:
                        ScrollViewer.LineDown();
                        break;
                    case Key.Up:
                    case Key.PageUp:
                        ScrollViewer.LineUp();
                        break;
                    case Key.Home:
                        ScrollViewer.ScrollToHome();
                        break;
                    case Key.End:
                        ScrollViewer.ScrollToEnd();
                        break;
                }
            };

            PointerPressed += (_, e) =>
            {
                if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
                {
                    return;
                }

                // Context menu doesn't want to be opened normally
                MainContextMenu.Open();
            };

            CloseItem.Click += (_, _) =>
            {
                if (TopLevel.GetTopLevel(this) is Window hostWindow)
                {
                    hostWindow.Close();
                }
            };

            PixelWidthTextBox.KeyDown += async (s, e) => await ResizeImageOnEnter(s, e);
            PixelHeightTextBox.KeyDown += async (s, e) => await ResizeImageOnEnter(s, e);

            PixelWidthTextBox.KeyUp += delegate { AdjustAspectRatio(PixelWidthTextBox); };
            PixelHeightTextBox.KeyUp += delegate { AdjustAspectRatio(PixelHeightTextBox); };

            vm.WindowTabs.ActiveTab.CurrentValue.FileInfo.SubscribeAwait(FileInfoAsyncSubscription)
                .AddTo(ref _disposables);

            SizeChanged += (_, _) => ResponsiveResizeUpdate(vm);
            
            FileNameTextBox.KeyDown += async (_, e) =>
                await HandleRenameOnEnterAsync(e, () =>
                    Path.Combine(vm.WindowTabs.ActiveTab.Value.Model.FileInfo.DirectoryName!, FileNameTextBox.Text));

            FullPathTextBox.KeyDown += async (_, e) =>
                await HandleRenameOnEnterAsync(e, () => FullPathTextBox.Text ?? string.Empty);

            DirectoryNameTextBox.KeyDown += async (_, e) =>
                await HandleRenameOnEnterAsync(e, () =>
                    Path.Combine(DirectoryNameTextBox.Text, vm.WindowTabs.ActiveTab.Value.Model.FileInfo.Name));

            // Orientation is for display only atm
            OrientationBox.DropDownClosed += (_, _) =>
            {
                OrientationBox.SelectedIndex = vm.Exif.Orientation.Value!;
            };
            
            // Resolution Units are for display only atm
            ResolutionUnitBox.DropDownClosed += (_, _) =>
            {
                ResolutionUnitBox.SelectedIndex = (int)vm.Exif.ResolutionUnit.Value!;
            };

            ColorRepresentationBox.DropDownClosed += async (_, _) =>
            {
                await AddExifPropertyAsync(ExifWriter.AddColorSpace, vm.Exif.ColorRepresentation.CurrentValue);
            };
            
            CompressionBox.DropDownClosed  += async (_, _) =>
            {
                await AddExifPropertyAsync(ExifWriter.AddCompression, vm.Exif.Compression.CurrentValue);
            };

            vm.InfoWindow.IsLoading.Value = false;
        };
    }

    private async ValueTask FileInfoAsyncSubscription(FileInfo? fileInfo, CancellationToken ct)
    {
        if (fileInfo is null || DataContext  is not MainWindowViewModel vm)
        {
            return;
        }
        
        var model = vm.WindowTabs.ActiveTab.CurrentValue.Model;
        await UpdateValuesAsync(model, ct).ConfigureAwait(false);
    }

    private async Task HandleRenameOnEnterAsync(KeyEventArgs e, Func<string> getNewPath)
    {
        if (e.Key is not Key.Enter || DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        
        try
        {
            var newPath = getNewPath();
            if (string.IsNullOrWhiteSpace(newPath))
            {
                return;
            }
        
            await Dispatcher.UIThread.InvokeAsync(() => SetLoadingState(true));
            vm.IsLoadingIndicatorShown.Value = true;
        
            var oldPath = vm.WindowTabs.ActiveTab.Value.FileInfo.CurrentValue.FullName;
        
            // Avoid renaming if the path hasn't changed
            if (oldPath.Equals(newPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            
            var isRenamed = await RenameHelper.RenameAction(vm, newPath);
            if (isRenamed)
            {
                await UpdateValuesAsync(vm.WindowTabs.ActiveTab.Value.Model, CancellationToken.None);
            }
            
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => SetLoadingState(false));
            vm.IsLoadingIndicatorShown.Value = false;
        }
    }


    private void ResponsiveResizeUpdate(MainWindowViewModel vm)
    {
        if (!Application.Current.TryGetResource("ScrollBarThickness", Application.Current.ActualThemeVariant,
                out var value))
        {
            return;
        }

        if (value is not double scrollBarThickness)
        {
            return;
        }

        var panelWidth = double.IsNaN(ParentPanel.Width) ? ParentPanel.Bounds.Width : ParentPanel.Width;
        panelWidth = panelWidth is 0 ? MinWidth : panelWidth;

        vm.InfoWindow.ResponsiveResizeUpdate(panelWidth, scrollBarThickness);
    }

    private async ValueTask UpdateValuesAsync(ImageModel imageModel, CancellationToken cancellationToken)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        await Task.Run(() =>
        {
            vm.Exif.UpdateExifValues(imageModel);
        }, cancellationToken);
        if (DirectoryNameTextBox.Text != imageModel.FileInfo.DirectoryName)
        {
            DirectoryNameTextBox.Text = imageModel.FileInfo.DirectoryName;
        }

        var tab = vm.WindowTabs.ActiveTab.Value;
        FileSizeBox.Text = tab.Model.FileInfo?.Length.GetReadableFileSize();
        tab.ShouldOptimizeImageBeEnabled.Value = ConversionHelper.DetermineIfOptimizeImageShouldBeEnabled(tab.Model.FileInfo);
        GoogleLinkButton.IsEnabled = !string.IsNullOrWhiteSpace(vm.Exif.GoogleLink.CurrentValue);
        BingLinkButton.IsEnabled = !string.IsNullOrWhiteSpace(vm.Exif.BingLink.CurrentValue);

        vm.Exif.IsExifAvailable.Value = vm.Exif.ImageFormat.CurrentValue.IsExifImage();
    }
    
    private void SetLoadingState(bool isLoading)
    {
        ParentPanel.Opacity = isLoading ? 0.1 : 1;
        ParentPanel.IsHitTestVisible = !isLoading;
        SpinWaiter.IsVisible = isLoading;
    }

    private void AdjustAspectRatio(TextBox sender)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var pixelWidth = vm.WindowTabs.ActiveTab.Value.Model.PixelWidth;
        var pixelHeight = vm.WindowTabs.ActiveTab.Value.Model.PixelHeight;
        var aspectRatio = (double)pixelWidth / pixelHeight;
        AspectRatioHelper.SetAspectRatioForTextBox(PixelWidthTextBox, PixelHeightTextBox, sender == PixelWidthTextBox, aspectRatio,
            pixelWidth, pixelHeight);

        if (!uint.TryParse(PixelWidthTextBox.Text, out var width) ||
            !uint.TryParse(PixelHeightTextBox.Text, out var height))
        {
            return;
        }

        if (width <= 0 || height <= 0)
        {
            return;
        }

        var printSizes =
            PrintSizing.GetPrintSizes(width, height, vm.Exif.DpiX.CurrentValue, vm.Exif.DpiY.CurrentValue);
        PrintSizeInchTextBox.Text = printSizes.PrintSizeInch;
        PrintSizeCmTextBox.Text = printSizes.PrintSizeCm;
        SizeMpTextBox.Text = printSizes.SizeMp;

        var gcd = AspectRatioFormatter.GCD(width, height);
        AspectRatioTextBox.Text =
            AspectRatioFormatter.GetFormattedAspectRatio(gcd, vm.WindowTabs.ActiveTab.Value.Model.PixelWidth,
                vm.WindowTabs.ActiveTab.Value.Model.PixelHeight);
    }

    private static async Task DoResize(MainWindowViewModel vm, bool isWidth, object width, object height)
    {
        if (isWidth)
        {
            if (!double.TryParse((string?)width, out var widthValue))
            {
                return;
            }

            if (widthValue > 0)
            {
                var success = await ConversionHelper.ResizeByWidth(vm.WindowTabs.ActiveTab.Value.Model.FileInfo, widthValue)
                    .ConfigureAwait(false);
                if (success)
                {
                    await vm.WindowTabs.ActiveTab.CurrentValue.ImageIterator.ReloadAsync().ConfigureAwait(false);
                }
            }
        }
        else
        {
            if (!double.TryParse((string?)height, out var heightValue))
            {
                return;
            }

            if (heightValue > 0)
            {
                var success = await ConversionHelper.ResizeByHeight(vm.WindowTabs.ActiveTab.Value.Model.FileInfo, heightValue)
                    .ConfigureAwait(false);
                if (success)
                {
                    await vm.WindowTabs.ActiveTab.CurrentValue.ImageIterator.ReloadAsync().ConfigureAwait(false);
                }
            }
        }
    }

    private async Task ResizeImageOnEnter(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is not MainWindowViewModel vm)
            {
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() => SetLoadingState(true));
            try
            {
                await DoResize(vm, Equals(sender, PixelWidthTextBox), PixelWidthTextBox.Text, PixelHeightTextBox.Text)
                    .ConfigureAwait(false);
            }
            finally
            {
                await Dispatcher.UIThread.InvokeAsync(() => SetLoadingState(false));
            }
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _disposables.Dispose();
    }

    private async Task AddExifPropertyAsync<T>(Func<FileInfo?, T, Task<bool>> addAction, T value)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            await addAction(vm.WindowTabs.ActiveTab.Value.Model.FileInfo, value);
        }
    }
}