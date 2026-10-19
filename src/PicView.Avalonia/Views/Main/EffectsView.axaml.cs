using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.Main;

public partial class EffectsView : UserControl
{
    private DisposableBag _disposables;

    public EffectsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DetachedFromLogicalTree += OnDetachedFromLogicalTree;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        if (DataContext is not CoreViewModel { Effects: { } effects } core)
        {
            return;
        }

        PointerPressed += OnPointerPressed;
        ResetButton.Click += async (_, _) => await RemoveEffects().ConfigureAwait(false);
        ClearEffectsItem.Click += async (_, _) => await RemoveEffects().ConfigureAwait(false);
        CancelButton.Click += (_, _) => CloseWindow();
        CloseItem.Click += (_, _) => CloseWindow();

        effects.Rendered
            .Subscribe(magick =>
                core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ActiveTab.Value.Image.Value =
                    magick.ToWriteableBitmap(), DebugHelper.LogError(nameof(EffectsView), nameof(effects.Rendered)))
            .AddTo(ref _disposables);
    }

    private void MoveWindow(object? sender, PointerPressedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window window)
        {
            window.BeginMoveDrag(e);
        }
    }

    private void CloseWindow()
    {
        if (TopLevel.GetTopLevel(this) is Window window)
        {
            window.Close();
        }
    }

    public async Task RemoveEffects()
    {
        if (DataContext is not CoreViewModel { Effects: { } effects } core)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(effects.DiscardEffects);
        await core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ReloadAsync().ConfigureAwait(false);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            ContextMenu?.Open();
        }
    }

    private void OnDetachedFromLogicalTree(object? sender, LogicalTreeAttachmentEventArgs e)
    {
        _disposables.Dispose();
    }
}
