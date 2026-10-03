using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PicView.Avalonia.Resizing;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.Main;
using PicView.Core.Exif;
using PicView.Core.Extensions;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Controllers;

public class ImageInfoViewController(ImageInfoView view) : IDisposable
{
    private DisposableBag _disposables;
    private bool _isDisposed;

    public void Initialize()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (view.DataContext is not MainWindowViewModel vm || vm.InfoWindow is null)
            {
                return;
            }

            ResponsiveResizeUpdate(vm);

            view.KeyDown += OnKeyDown;
            view.PointerPressed += OnPointerPressed;
            view.CloseItem.Click += OnCloseClick;
            view.PixelWidthTextBox.KeyDown += OnPixelWidthKeyDown;
            view.PixelHeightTextBox.KeyDown += OnPixelHeightKeyDown;
            view.PixelWidthTextBox.KeyUp += OnPixelWidthKeyUp;
            view.PixelHeightTextBox.KeyUp += OnPixelHeightKeyUp;
            view.SizeChanged += OnSizeChanged;
            view.FileNameTextBox.KeyDown += OnFileNameKeyDown;
            view.FullPathTextBox.KeyDown += OnFullPathKeyDown;
            view.DirectoryNameTextBox.KeyDown += OnDirectoryNameKeyDown;
            view.OrientationBox.DropDownClosed += OnOrientationBoxDropDownClosed;
            view.ResolutionUnitBox.DropDownClosed += OnResolutionUnitBoxDropDownClosed;
            view.ColorRepresentationBox.DropDownClosed += OnColorRepresentationBoxDropDownClosed;
            view.CompressionBox.DropDownClosed += OnCompressionBoxDropDownClosed;

            vm.WindowTabs.ActiveTab.CurrentValue.FileInfo.SubscribeAwait(async (fileInfo, ct) => 
            {
                if (fileInfo != null) 
                {
                    var model = vm.WindowTabs.ActiveTab.CurrentValue.Model;
                    await vm.InfoWindow.UpdateValuesAsync(model, ct).ConfigureAwait(false);
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (!string.Equals(view.DirectoryNameTextBox.Text, model.FileInfo.DirectoryName, StringComparison.Ordinal))
                        {
                            view.DirectoryNameTextBox.Text = model.FileInfo.DirectoryName;
                        }
                        view.FileSizeBox.Text = model.FileInfo?.Length.GetReadableFileSize();
                        view.GoogleLinkButton.IsEnabled = !string.IsNullOrWhiteSpace(vm.Exif?.GoogleLink?.CurrentValue);
                        view.BingLinkButton.IsEnabled = !string.IsNullOrWhiteSpace(vm.Exif?.BingLink?.CurrentValue);
                    });
                }
            }).AddTo(ref _disposables);

            vm.InfoWindow.IsLoading.Value = false;
        }, DispatcherPriority.Background);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
            case Key.PageDown:
                view.ScrollViewer.LineDown();
                break;
            case Key.Up:
            case Key.PageUp:
                view.ScrollViewer.LineUp();
                break;
            case Key.Home:
                view.ScrollViewer.ScrollToHome();
                break;
            case Key.End:
                view.ScrollViewer.ScrollToEnd();
                break;
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(view).Properties.IsRightButtonPressed)
        {
            return;
        }
        view.MainContextMenu.Open();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(view) is Window hostWindow)
        {
            hostWindow.Close();
        }
    }

    private void OnPixelWidthKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && view.DataContext is MainWindowViewModel { InfoWindow: not null } vm)
        {
            _ = Task.Run(() => vm.InfoWindow.ResizeImageAsync(true, view.PixelWidthTextBox.Text, view.PixelHeightTextBox.Text));
        }
    }

    private void OnPixelHeightKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && view.DataContext is MainWindowViewModel { InfoWindow: not null } vm)
        {
            _ = Task.Run(() => vm.InfoWindow.ResizeImageAsync(false, view.PixelWidthTextBox.Text, view.PixelHeightTextBox.Text));
        }
    }

    private void OnPixelWidthKeyUp(object? sender, KeyEventArgs e)
    {
        AdjustAspectRatio(view.PixelWidthTextBox);
    }

    private void OnPixelHeightKeyUp(object? sender, KeyEventArgs e)
    {
        AdjustAspectRatio(view.PixelHeightTextBox);
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (view.DataContext is MainWindowViewModel vm)
        {
            ResponsiveResizeUpdate(vm);
        }
    }

    private void OnFileNameKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not Key.Enter || view.DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var newPath = Path.Combine(vm.WindowTabs.ActiveTab.Value.Model.FileInfo.DirectoryName!, view.FileNameTextBox.Text);
        _ = Task.Run(() => HandleRename(vm, newPath));
    }

    private void OnFullPathKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter && view.DataContext is MainWindowViewModel vm)
        {
            _ = Task.Run(() => HandleRename(vm, view.FullPathTextBox.Text ?? string.Empty));
        }
    }

    private void OnDirectoryNameKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not Key.Enter || view.DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var newPath = Path.Combine(view.DirectoryNameTextBox.Text, vm.WindowTabs.ActiveTab.Value.Model.FileInfo.Name);
        _ = Task.Run(() => HandleRename(vm, newPath));
    }

    private void OnOrientationBoxDropDownClosed(object? sender, EventArgs e)
    {
        if (view.DataContext is MainWindowViewModel vm && vm.Exif?.Orientation.Value != null)
        {
            view.OrientationBox.SelectedIndex = vm.Exif.Orientation.Value;
        }
    }

    private void OnResolutionUnitBoxDropDownClosed(object? sender, EventArgs e)
    {
        if (view.DataContext is MainWindowViewModel vm && vm.Exif?.ResolutionUnit.Value != null)
        {
            view.ResolutionUnitBox.SelectedIndex = (int)vm.Exif.ResolutionUnit.Value;
        }
    }

    private void OnColorRepresentationBoxDropDownClosed(object? sender, EventArgs e)
    {
        if (view.DataContext is MainWindowViewModel { InfoWindow: not null } vm)
        {
            _ = Task.Run(() => vm.InfoWindow.AddExifPropertyAsync(ExifWriter.AddColorSpace, vm.Exif.ColorRepresentation.CurrentValue));
        }
    }

    private void OnCompressionBoxDropDownClosed(object? sender, EventArgs e)
    {
        if (view.DataContext is MainWindowViewModel { InfoWindow: not null } vm)
        {
            _ = Task.Run(() => vm.InfoWindow.AddExifPropertyAsync(ExifWriter.AddCompression, vm.Exif.Compression.CurrentValue));
        }
    }

    private void SetLoadingState(bool isLoading)
    {
        view.ParentPanel.Opacity = isLoading ? 0.1 : 1;
        view.ParentPanel.IsHitTestVisible = !isLoading;
        view.SpinWaiter.IsVisible = isLoading;
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
                    if (!string.Equals(view.DirectoryNameTextBox.Text, model.FileInfo.DirectoryName, StringComparison.Ordinal))
                    {
                        view.DirectoryNameTextBox.Text = model.FileInfo.DirectoryName;
                    }
                    view.FileSizeBox.Text = model.FileInfo?.Length.GetReadableFileSize();
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

        var panelWidth = double.IsNaN(view.ParentPanel.Width) ? view.ParentPanel.Bounds.Width : view.ParentPanel.Width;
        panelWidth = panelWidth is 0 ? view.MinWidth : panelWidth;

        vm.InfoWindow?.ResponsiveResizeUpdate(panelWidth, scrollBarThickness);
    }

    private void AdjustAspectRatio(TextBox sender)
    {
        if (view.DataContext is not MainWindowViewModel vm || vm.InfoWindow is null)
        {
            return;
        }

        var pixelWidth = vm.WindowTabs.ActiveTab.Value.Model.PixelWidth;
        var pixelHeight = vm.WindowTabs.ActiveTab.Value.Model.PixelHeight;
        var aspectRatio = (double)pixelWidth / pixelHeight;
        AspectRatioHelper.SetAspectRatioForTextBox(view.PixelWidthTextBox, view.PixelHeightTextBox, sender == view.PixelWidthTextBox, aspectRatio,
            pixelWidth, pixelHeight);

        if (!uint.TryParse(view.PixelWidthTextBox.Text, out var width) ||
            !uint.TryParse(view.PixelHeightTextBox.Text, out var height))
        {
            return;
        }

        if (width <= 0 || height <= 0)
        {
            return;
        }
        
        vm.InfoWindow.UpdatePrintSizes(width, height);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;

        view.KeyDown -= OnKeyDown;
        view.PointerPressed -= OnPointerPressed;
        view.CloseItem.Click -= OnCloseClick;
        view.PixelWidthTextBox.KeyDown -= OnPixelWidthKeyDown;
        view.PixelHeightTextBox.KeyDown -= OnPixelHeightKeyDown;
        view.PixelWidthTextBox.KeyUp -= OnPixelWidthKeyUp;
        view.PixelHeightTextBox.KeyUp -= OnPixelHeightKeyUp;
        view.SizeChanged -= OnSizeChanged;
        view.FileNameTextBox.KeyDown -= OnFileNameKeyDown;
        view.FullPathTextBox.KeyDown -= OnFullPathKeyDown;
        view.DirectoryNameTextBox.KeyDown -= OnDirectoryNameKeyDown;
        view.OrientationBox.DropDownClosed -= OnOrientationBoxDropDownClosed;
        view.ResolutionUnitBox.DropDownClosed -= OnResolutionUnitBoxDropDownClosed;
        view.ColorRepresentationBox.DropDownClosed -= OnColorRepresentationBoxDropDownClosed;
        view.CompressionBox.DropDownClosed -= OnCompressionBoxDropDownClosed;

        _disposables.Dispose();
        GC.SuppressFinalize(this);
    }
}

