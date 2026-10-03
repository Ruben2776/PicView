using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PicView.Avalonia.Resizing;
using PicView.Avalonia.UI;
using PicView.Core.ViewModels;
using PicView.Core.Exif;
using PicView.Core.Extensions;
using R3;

namespace PicView.Avalonia.Views.Main;

public partial class ImageInfoView : UserControl
{
    private DisposableBag _disposables;

    public ImageInfoView()
    {
        InitializeComponent();
        Loaded += HandleLoaded;
    }
    
    private void HandleLoaded(object? sender, RoutedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (DataContext is not MainWindowViewModel vm || vm.InfoWindow is null)
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

                MainContextMenu.Open();
            };

            CloseItem.Click += (_, _) =>
            {
                if (TopLevel.GetTopLevel(this) is Window hostWindow)
                {
                    hostWindow.Close();
                }
            };

            PixelWidthTextBox.KeyDown += async (s, e) => 
            {
                if (e.Key == Key.Enter)
                {
                    await vm.InfoWindow.ResizeImageAsync(true, PixelWidthTextBox.Text, PixelHeightTextBox.Text).ConfigureAwait(false);
                }
            };
            PixelHeightTextBox.KeyDown += async (s, e) => 
            {
                if (e.Key == Key.Enter)
                {
                    await vm.InfoWindow.ResizeImageAsync(false, PixelWidthTextBox.Text, PixelHeightTextBox.Text).ConfigureAwait(false);
                }
            };

            PixelWidthTextBox.KeyUp += delegate { AdjustAspectRatio(PixelWidthTextBox); };
            PixelHeightTextBox.KeyUp += delegate { AdjustAspectRatio(PixelHeightTextBox); };

            vm.WindowTabs.ActiveTab.CurrentValue.FileInfo.SubscribeAwait(async (fileInfo, ct) => 
            {
                if (fileInfo != null) 
                {
                    var model = vm.WindowTabs.ActiveTab.CurrentValue.Model;
                    await vm.InfoWindow.UpdateValuesAsync(model, ct).ConfigureAwait(false);
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (!string.Equals(DirectoryNameTextBox.Text, model.FileInfo.DirectoryName, StringComparison.Ordinal))
                        {
                            DirectoryNameTextBox.Text = model.FileInfo.DirectoryName;
                        }
                        FileSizeBox.Text = model.FileInfo?.Length.GetReadableFileSize();
                        GoogleLinkButton.IsEnabled = !string.IsNullOrWhiteSpace(vm.Exif?.GoogleLink?.CurrentValue);
                        BingLinkButton.IsEnabled = !string.IsNullOrWhiteSpace(vm.Exif?.BingLink?.CurrentValue);
                    });
                }
            }).AddTo(ref _disposables);

            SizeChanged += (_, _) => ResponsiveResizeUpdate(vm);
            
            FileNameTextBox.KeyDown += async (_, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    var newPath = Path.Combine(vm.WindowTabs.ActiveTab.Value.Model.FileInfo.DirectoryName!, FileNameTextBox.Text);
                    await HandleRename(vm, newPath).ConfigureAwait(false);
                }
            };

            FullPathTextBox.KeyDown += async (_, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    await HandleRename(vm, FullPathTextBox.Text ?? string.Empty).ConfigureAwait(false);
                }
            };

            DirectoryNameTextBox.KeyDown += async (_, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    var newPath = Path.Combine(DirectoryNameTextBox.Text, vm.WindowTabs.ActiveTab.Value.Model.FileInfo.Name);
                    await HandleRename(vm, newPath).ConfigureAwait(false);
                }
            };

            OrientationBox.DropDownClosed += (_, _) =>
            {
                if (vm.Exif?.Orientation.Value != null)
                {
                    OrientationBox.SelectedIndex = vm.Exif.Orientation.Value;
                }
            };
            
            ResolutionUnitBox.DropDownClosed += (_, _) =>
            {
                if (vm.Exif?.ResolutionUnit.Value != null)
                {
                    ResolutionUnitBox.SelectedIndex = (int)vm.Exif.ResolutionUnit.Value;
                }
            };

            ColorRepresentationBox.DropDownClosed += async (_, _) =>
            {
                await vm.InfoWindow.AddExifPropertyAsync(ExifWriter.AddColorSpace, vm.Exif.ColorRepresentation.CurrentValue).ConfigureAwait(false);
            };
            
            CompressionBox.DropDownClosed  += async (_, _) =>
            {
                await vm.InfoWindow.AddExifPropertyAsync(ExifWriter.AddCompression, vm.Exif.Compression.CurrentValue).ConfigureAwait(false);
            };

            vm.InfoWindow.IsLoading.Value = false;
        }, DispatcherPriority.Background);
    }
    
    private void SetLoadingState(bool isLoading)
    {
        ParentPanel.Opacity = isLoading ? 0.1 : 1;
        ParentPanel.IsHitTestVisible = !isLoading;
        SpinWaiter.IsVisible = isLoading;
    }

    private async Task HandleRename(MainWindowViewModel vm, string newPath)
    {
        if (string.IsNullOrWhiteSpace(newPath))
        {
            return;
        }

        var oldPath = vm.WindowTabs.ActiveTab.Value.FileInfo.CurrentValue.FullName;
        if (oldPath.Equals(newPath, StringComparison.OrdinalIgnoreCase)) return;

        await Dispatcher.UIThread.InvokeAsync(() => SetLoadingState(true));
        vm.IsLoadingIndicatorShown.Value = true;
        try
        {
            var isRenamed = await RenameHelper.RenameAction(vm, newPath).ConfigureAwait(false);
            if (isRenamed)
            {
                await vm.InfoWindow!.UpdateValuesAsync(vm.WindowTabs.ActiveTab.Value.Model, CancellationToken.None).ConfigureAwait(false);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var model = vm.WindowTabs.ActiveTab.Value.Model;
                    if (!string.Equals(DirectoryNameTextBox.Text, model.FileInfo.DirectoryName, StringComparison.Ordinal))
                    {
                        DirectoryNameTextBox.Text = model.FileInfo.DirectoryName;
                    }
                    FileSizeBox.Text = model.FileInfo?.Length.GetReadableFileSize();
                });
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

        vm.InfoWindow?.ResponsiveResizeUpdate(panelWidth, scrollBarThickness);
    }

    private void AdjustAspectRatio(TextBox sender)
    {
        if (DataContext is not MainWindowViewModel vm || vm.InfoWindow is null)
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
        
        vm.InfoWindow.UpdatePrintSizes(width, height);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _disposables.Dispose();
    }
}

