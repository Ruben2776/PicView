using Avalonia;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Functions;
using PicView.Core.DebugTools;
using PicView.Core.ViewModels;
using R3;

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
                await vm.Mapper.ResizeImage().ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(QuickEditingDialog), nameof(FunctionsMapper.ResizeImage)))
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
                await vm.Mapper.BatchResizeWindow().ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(QuickEditingDialog), nameof(FunctionsMapper.BatchResizeWindow)))
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
                await vm.Mapper.ImageInfoWindow().ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(QuickEditingDialog), nameof(FunctionsMapper.ImageInfoWindow)))
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
                await vm.Mapper.EffectsWindow().ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(QuickEditingDialog), nameof(FunctionsMapper.EffectsWindow)))
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
                await vm.Mapper.Crop().ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(QuickEditingDialog), nameof(FunctionsMapper.Crop)))
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
                await vm.Mapper.Slideshow().ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(QuickEditingDialog), nameof(FunctionsMapper.Slideshow)))
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
                await vm.Mapper.OpenCloseDockedGallery().ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(QuickEditingDialog), nameof(FunctionsMapper.OpenCloseDockedGallery)))
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
                await vm.Mapper.SideBySide().ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(QuickEditingDialog), nameof(FunctionsMapper.SideBySide)))
            .AddTo(ref _disposables);
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        Loaded -= OnLoaded;
        _disposables.Dispose();
    }
}