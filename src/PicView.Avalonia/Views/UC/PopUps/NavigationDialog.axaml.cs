using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using PicView.Avalonia.CustomControls;
using PicView.Core.DebugTools;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.UC.PopUps;

public partial class NavigationDialog : AnimatedPopUp
{
    private DisposableBag _disposables;
    public NavigationDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        if (!Settings.Theme.Dark)
        {
            ApplyLightTheme();
        }
    }
    
    private void ApplyLightTheme()
    {
        NextButton.Classes.Remove("MenuItemHover");
        NextButton.Classes.Add("hover");
        
        PrevButton.Classes.Remove("MenuItemHover");
        PrevButton.Classes.Add("hover");

        Next10Button.Classes.Remove("MenuItemHover");
        Next10Button.Classes.Add("hover");
        
        Next100Button.Classes.Remove("MenuItemHover");
        Next100Button.Classes.Add("hover");
        
        Prev10Button.Classes.Remove("MenuItemHover");
        Prev10Button.Classes.Add("hover");
        
        Prev100Button.Classes.Remove("MenuItemHover");
        Prev100Button.Classes.Add("hover");
        
        FirstImageButton.Classes.Remove("MenuItemHover");
        FirstImageButton.Classes.Add("hover");
        
        LastImageButton.Classes.Remove("MenuItemHover");
        LastImageButton.Classes.Add("hover");
        
        NextFolderButton.Classes.Remove("MenuItemHover");
        NextFolderButton.Classes.Add("hover");
        
        PrevFolderButton.Classes.Remove("MenuItemHover");
        PrevFolderButton.Classes.Add("hover");

        NextArchiveButton.Classes.Remove("MenuItemHover");
        NextArchiveButton.Classes.Add("hover");

        PrevArchiveButton.Classes.Remove("MenuItemHover");
        PrevArchiveButton.Classes.Add("hover");
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Observable.FromEventHandler<RoutedEventArgs>(h => NextButton.Click += h,
                h => NextButton.Click -= h)
            .SubscribeAwait(async (s, c) =>
            {
                _ = AnimatedClosing();
                if (DataContext is not MainWindowViewModel vm)
                {
                    return;
                }

                await vm.WindowTabs.NextFile();
            }, static result =>
            {
#if DEBUG
                if (result is { IsFailure: true, Exception: not null })
                {
                    DebugHelper.LogDebug(nameof(NavigationDialog), nameof(OnLoaded), result.Exception);
                }
#endif
            }, AwaitOperation.Drop)
            .AddTo(ref _disposables);
        
        Observable.FromEventHandler<RoutedEventArgs>(h => PrevButton.Click += h,
                h => PrevButton.Click -= h)
            .SubscribeAwait(async (s, c) =>
            {
                _ = AnimatedClosing();
                if (DataContext is not MainWindowViewModel vm)
                {
                    return;
                }

                await vm.WindowTabs.PrevFile();
            }, static result =>
            {
#if DEBUG
                if (result is { IsFailure: true, Exception: not null })
                {
                    DebugHelper.LogDebug(nameof(NavigationDialog), nameof(OnLoaded), result.Exception);
                }
#endif
            }, AwaitOperation.Drop)
            .AddTo(ref _disposables);
        
        Observable.FromEventHandler<RoutedEventArgs>(h => Next10Button.Click += h,
                h => Next10Button.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                if (DataContext is not MainWindowViewModel vm)
                {
                    return;
                }

                await vm.WindowTabs.Next10();
            }, static result =>
            {
#if DEBUG
                if (result is { IsFailure: true, Exception: not null })
                {
                    DebugHelper.LogDebug(nameof(NavigationDialog), nameof(OnLoaded), result.Exception);
                }
#endif
            }, AwaitOperation.Drop)
            .AddTo(ref _disposables);
        
        Observable.FromEventHandler<RoutedEventArgs>(h => Next100Button.Click += h,
                h => Next100Button.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                if (DataContext is not MainWindowViewModel vm)
                {
                    return;
                }

                await vm.WindowTabs.Next100();
            }, static result =>
            {
#if DEBUG
                if (result is { IsFailure: true, Exception: not null })
                {
                    DebugHelper.LogDebug(nameof(NavigationDialog), nameof(OnLoaded), result.Exception);
                }
#endif
            }, AwaitOperation.Drop)
            .AddTo(ref _disposables);
        
        Observable.FromEventHandler<RoutedEventArgs>(h => Prev10Button.Click += h,
                h => Prev10Button.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                if (DataContext is not MainWindowViewModel vm)
                {
                    return;
                }

                await vm.WindowTabs.Prev10();
            }, static result =>
            {
#if DEBUG
                if (result is { IsFailure: true, Exception: not null })
                {
                    DebugHelper.LogDebug(nameof(NavigationDialog), nameof(OnLoaded), result.Exception);
                }
#endif
            }, AwaitOperation.Drop)
            .AddTo(ref _disposables);
        
        Observable.FromEventHandler<RoutedEventArgs>(h => Prev100Button.Click += h,
                h => Prev100Button.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                if (DataContext is not MainWindowViewModel vm)
                {
                    return;
                }

                await vm.WindowTabs.Prev100();
            }, static result =>
            {
#if DEBUG
                if (result is { IsFailure: true, Exception: not null })
                {
                    DebugHelper.LogDebug(nameof(NavigationDialog), nameof(OnLoaded), result.Exception);
                }
#endif
            }, AwaitOperation.Drop)
            .AddTo(ref _disposables);
        
        Observable.FromEventHandler<RoutedEventArgs>(h => FirstImageButton.Click += h,
                h => FirstImageButton.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                if (DataContext is not MainWindowViewModel vm)
                {
                    return;
                }

                await vm.WindowTabs.FirstFile();
            }, static result =>
            {
#if DEBUG
                if (result is { IsFailure: true, Exception: not null })
                {
                    DebugHelper.LogDebug(nameof(NavigationDialog), nameof(OnLoaded), result.Exception);
                }
#endif
            }, AwaitOperation.Drop)
            .AddTo(ref _disposables);
        
        Observable.FromEventHandler<RoutedEventArgs>(h => LastImageButton.Click += h,
                h => LastImageButton.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                if (DataContext is not MainWindowViewModel vm)
                {
                    return;
                }

                await vm.WindowTabs.LastFile();
            }, static result =>
            {
#if DEBUG
                if (result is { IsFailure: true, Exception: not null })
                {
                    DebugHelper.LogDebug(nameof(NavigationDialog), nameof(OnLoaded), result.Exception);
                }
#endif
            }, AwaitOperation.Drop)
            .AddTo(ref _disposables);
        
        Observable.FromEventHandler<RoutedEventArgs>(h => NextFolderButton.Click += h,
                h => NextFolderButton.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                // TODO add next folder
            }, static result =>
            {
#if DEBUG
                if (result is { IsFailure: true, Exception: not null })
                {
                    DebugHelper.LogDebug(nameof(NavigationDialog), nameof(OnLoaded), result.Exception);
                }
#endif
            }, AwaitOperation.Drop)
            .AddTo(ref _disposables);
        
        Observable.FromEventHandler<RoutedEventArgs>(h => PrevFolderButton.Click += h,
                h => PrevFolderButton.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                // TODO add prev folder
            }, static result =>
            {
#if DEBUG
                if (result is { IsFailure: true, Exception: not null })
                {
                    DebugHelper.LogDebug(nameof(NavigationDialog), nameof(OnLoaded), result.Exception);
                }
#endif
            }, AwaitOperation.Drop)
            .AddTo(ref _disposables);


        Observable.FromEventHandler<RoutedEventArgs>(h => NextArchiveButton.Click += h,
                h => NextFolderButton.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                // TODO add next archive
            }, static result =>
            {
#if DEBUG
                if (result is { IsFailure: true, Exception: not null })
                {
                    DebugHelper.LogDebug(nameof(NavigationDialog), nameof(OnLoaded), result.Exception);
                }
#endif
            }, AwaitOperation.Drop)
            .AddTo(ref _disposables);

        Observable.FromEventHandler<RoutedEventArgs>(h => PrevArchiveButton.Click += h,
                h => PrevFolderButton.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                // TODO add prev archive
            }, static result =>
            {
#if DEBUG
                if (result is { IsFailure: true, Exception: not null })
                {
                    DebugHelper.LogDebug(nameof(NavigationDialog), nameof(OnLoaded), result.Exception);
                }
#endif
            }, AwaitOperation.Drop)
            .AddTo(ref _disposables);
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        Loaded -= OnLoaded;
        // Dispose current subscriptions but keep the CompositeDisposable reusable
        _disposables.Dispose();
    }
}