using Avalonia;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using PicView.Avalonia.CustomControls;
using PicView.Core.DebugTools;
using PicView.Core.ViewModels;
using R3;
using PicView.Core.Keybindings;

namespace PicView.Avalonia.Views.UC.PopUps;

public partial class QuickEditingDialog : AnimatedPopUp
{
    private DisposableBag _disposables;
    public QuickEditingDialog()
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        DataContext = core.MainWindows.ActiveWindow.CurrentValue;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        
        Observable.FromEventHandler<RoutedEventArgs>(h => SingleResizeButton.Click += h,
                h => SingleResizeButton.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                if (DataContext is not MainWindowViewModel vm)
                {
                    return;
                }
                await vm.Mapper.Run(Actions.ResizeImage).ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(QuickEditingDialog), Actions.ResizeImage))
            .AddTo(ref _disposables);
        
        Observable.FromEventHandler<RoutedEventArgs>(h => BatchResizeButton.Click += h,
                h => BatchResizeButton.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                if (DataContext is not MainWindowViewModel vm)
                {
                    return;
                }
                await vm.Mapper.Run(Actions.BatchResizeWindow).ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(QuickEditingDialog), Actions.BatchResizeWindow))
            .AddTo(ref _disposables);
            
        Observable.FromEventHandler<RoutedEventArgs>(h => ImageInfoButton.Click += h,
                h => ImageInfoButton.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                if (DataContext is not MainWindowViewModel vm)
                {
                    return;
                }
                await vm.Mapper.Run(Actions.ImageInfoWindow).ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(QuickEditingDialog), Actions.ImageInfoWindow))
            .AddTo(ref _disposables);
        
        Observable.FromEventHandler<RoutedEventArgs>(h => ImageEffectsButton.Click += h,
                h => ImageEffectsButton.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                if (DataContext is not MainWindowViewModel vm)
                {
                    return;
                }
                await vm.Mapper.Run(Actions.EffectsWindow).ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(QuickEditingDialog), Actions.EffectsWindow))
            .AddTo(ref _disposables);
        
        Observable.FromEventHandler<RoutedEventArgs>(h => CropButton.Click += h,
                h => CropButton.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                if (DataContext is not MainWindowViewModel vm)
                {
                    return;
                }
                await vm.Mapper.Run(Actions.Crop).ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(QuickEditingDialog), Actions.Crop))
            .AddTo(ref _disposables);
        
        Observable.FromEventHandler<RoutedEventArgs>(h => SlideshowButton.Click += h,
                h => SlideshowButton.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                await AnimatedClosing().ConfigureAwait(false);
                if (DataContext is not MainWindowViewModel vm)
                {
                    return;
                }
                await vm.Mapper.Run(Actions.Slideshow).ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(QuickEditingDialog), Actions.Slideshow))
            .AddTo(ref _disposables);
                
        Observable.FromEventHandler<RoutedEventArgs>(h => ShowDockedGalleryButton.Click += h,
                h => ShowDockedGalleryButton.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                if (DataContext is not MainWindowViewModel vm)
                {
                    return;
                }
                await vm.Mapper.Run(Actions.OpenCloseDockedGallery).ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(QuickEditingDialog), Actions.OpenCloseDockedGallery))
            .AddTo(ref _disposables);
        
        Observable.FromEventHandler<RoutedEventArgs>(h => SideBySideButton.Click += h,
                h => SideBySideButton.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                if (DataContext is not MainWindowViewModel vm)
                {
                    return;
                }
                await vm.Mapper.Run(Actions.SideBySide).ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(QuickEditingDialog), Actions.SideBySide))
            .AddTo(ref _disposables);
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        Loaded -= OnLoaded;
        _disposables.Dispose();
    }
}