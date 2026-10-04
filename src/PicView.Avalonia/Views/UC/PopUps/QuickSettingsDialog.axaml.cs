using Avalonia;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Functions;
using PicView.Core.DebugTools;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.UC.PopUps;

public partial class QuickSettingsDialog : AnimatedPopUp
{
    private DisposableBag _disposables;
    public QuickSettingsDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Observable.FromEventHandler<RoutedEventArgs>(h => AllSettingsButton.Click += h,
                h => AllSettingsButton.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                if (Application.Current.DataContext is not CoreViewModel core)
                {
                    return;
                }

                await core.MainWindows.ActiveWindow.CurrentValue.Mapper.SettingsWindow().ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(QuickSettingsDialog), nameof(FunctionsMapper.SettingsWindow)))
            .AddTo(ref _disposables);
        
        Observable.FromEventHandler<RoutedEventArgs>(h => AboutButton.Click += h,
                h => AboutButton.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                if (Application.Current.DataContext is not CoreViewModel core)
                {
                    return;
                }

                await core.MainWindows.ActiveWindow.CurrentValue.Mapper.AboutWindow().ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(QuickSettingsDialog), nameof(FunctionsMapper.AboutWindow)))
            .AddTo(ref _disposables);
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        Loaded -= OnLoaded;
        _disposables.Dispose();
    }
}