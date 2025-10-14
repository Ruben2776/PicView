using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Functions;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using R3;

namespace PicView.Avalonia.Views.UC.PopUps;

public partial class NavigationDialog : AnimatedPopUp
{
    private readonly CompositeDisposable _subscriptions = new();
    public NavigationDialog()
    {
        DataContext = UIHelper.GetMainView.DataContext as MainViewModel;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Ensure we don't double-subscribe if Loaded fires multiple times
        _subscriptions.Clear();

        Observable.FromEventHandler<RoutedEventArgs>(h => NextButton.Click += h,
                h => NextButton.Click -= h)
            .SubscribeAwait(async (s, c) =>
            {
                _ = AnimatedClosing();
                await NavigationManager.Navigate(true, UIHelper.GetMainView.DataContext as MainViewModel, c);
            })
            .AddTo(_subscriptions);
        
        Observable.FromEventHandler<RoutedEventArgs>(h => PrevButton.Click += h,
                h => PrevButton.Click -= h)
            .SubscribeAwait(async (s, c) =>
            {
                _ = AnimatedClosing();
                await NavigationManager.Navigate(false, UIHelper.GetMainView.DataContext as MainViewModel, c);
            })
            .AddTo(_subscriptions);
        
        Observable.FromEventHandler<RoutedEventArgs>(h => Next10Button.Click += h,
                h => Next10Button.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                await FunctionsMapper.Next10();
            })
            .AddTo(_subscriptions);
        
        Observable.FromEventHandler<RoutedEventArgs>(h => Next100Button.Click += h,
                h => Next100Button.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                await FunctionsMapper.Next100();
            })
            .AddTo(_subscriptions);
        
        Observable.FromEventHandler<RoutedEventArgs>(h => Prev10Button.Click += h,
                h => Prev10Button.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                await FunctionsMapper.Prev10();
            })
            .AddTo(_subscriptions);
        
        Observable.FromEventHandler<RoutedEventArgs>(h => Prev100Button.Click += h,
                h => Prev100Button.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                await FunctionsMapper.Prev100();
            })
            .AddTo(_subscriptions);
        
        Observable.FromEventHandler<RoutedEventArgs>(h => FirstImageButton.Click += h,
                h => FirstImageButton.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                await FunctionsMapper.First();
            })
            .AddTo(_subscriptions);
        
        Observable.FromEventHandler<RoutedEventArgs>(h => LastImageButton.Click += h,
                h => LastImageButton.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                await FunctionsMapper.Last();
            })
            .AddTo(_subscriptions);
        
        Observable.FromEventHandler<RoutedEventArgs>(h => NextFolderButton.Click += h,
                h => NextFolderButton.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                await FunctionsMapper.NextFolder();
            })
            .AddTo(_subscriptions);
        
        Observable.FromEventHandler<RoutedEventArgs>(h => PrevFolderButton.Click += h,
                h => PrevFolderButton.Click -= h)
            .SubscribeAwait(async (_, _) =>
            {
                _ = AnimatedClosing();
                await FunctionsMapper.PrevFolder();
            })
            .AddTo(_subscriptions);
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        Loaded -= OnLoaded;
        // Dispose current subscriptions but keep the CompositeDisposable reusable
        _subscriptions.Clear();
    }
}