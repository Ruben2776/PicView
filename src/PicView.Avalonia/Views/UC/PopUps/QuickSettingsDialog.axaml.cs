using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using PicView.Avalonia.CustomControls;
using PicView.Core.DebugTools;
using PicView.Core.ViewModels;
using R3;
using PicView.Core.Keybindings;

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
                // Taken before closing, which takes the dialog out of its window
                var vm = (TopLevel.GetTopLevel(this) as MainWindow)?.ViewModel;
                _ = AnimatedClosing();
                if (vm is null)
                {
                    return;
                }

                await vm.ActionRunner.Run(Actions.SettingsWindow).ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(QuickSettingsDialog), Actions.SettingsWindow))
            .AddTo(ref _disposables);
        
        Observable.FromEventHandler<RoutedEventArgs>(h => AboutButton.Click += h,
                h => AboutButton.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                // Taken before closing, which takes the dialog out of its window
                var vm = (TopLevel.GetTopLevel(this) as MainWindow)?.ViewModel;
                _ = AnimatedClosing();
                if (vm is null)
                {
                    return;
                }

                await vm.ActionRunner.Run(Actions.AboutWindow).ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(QuickSettingsDialog), Actions.AboutWindow))
            .AddTo(ref _disposables);
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        Loaded -= OnLoaded;
        _disposables.Dispose();
    }
}