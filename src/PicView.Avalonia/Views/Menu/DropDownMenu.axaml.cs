using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using ObservableCollections;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Navigation;
using PicView.Core.DebugTools;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using R3;
using MainWindowViewModel = PicView.Core.ViewModels.MainWindowViewModel;

namespace PicView.Avalonia.Views.Menu;

public partial class DropDownMenu : AnimatedMenu
{
    private IDisposable? _menuVisibilitySubscription;
    private readonly MainWindow _mainWindow;

    public DropDownMenu(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        if (mainWindow.DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        vm.FileHistory ??= new FileHistoryViewModel(vm);
        DataContext = vm;
        InitializeComponent();
        Loaded += OnLoaded;

    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        SlideShow2Sec.Text = $"2 {TranslationManager.Translation.SecAbbreviation}";
        SlideShow5Sec.Text = $"5 {TranslationManager.Translation.SecAbbreviation}";
        SlideShow10Sec.Text = $"10 {TranslationManager.Translation.SecAbbreviation}";
        SlideShow20Sec.Text = $"20 {TranslationManager.Translation.SecAbbreviation}";
        SlideShow30Sec.Text = $"30 {TranslationManager.Translation.SecAbbreviation}";
        SlideShow60Sec.Text = $"60 {TranslationManager.Translation.SecAbbreviation}";
        SlideShow90Sec.Text = $"90 {TranslationManager.Translation.SecAbbreviation}";
        SlideShow120Sec.Text = $"120 {TranslationManager.Translation.SecAbbreviation}";
        
        SlideShow2Sec.Click += SlideShow2SecOnClick;
        SlideShow5Sec.Click += SlideShow5SecOnClick;
        SlideShow10Sec.Click += SlideShow10SecOnClick;
        SlideShow20Sec.Click += SlideShow20SecOnClick;
        SlideShow30Sec.Click += SlideShow30SecOnClick;
        SlideShow60Sec.Click += SlideShow60SecOnClick;
        SlideShow90Sec.Click += SlideShow90SecOnClick;
        SlideShow120Sec.Click += SlideShow120SecOnClick;
        
        if (_mainWindow.DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        vm.FileHistory.PinnedEntries.CollectionChanged += PinnedEntriesOnCollectionChanged;
        vm.FileHistory.Entries.CollectionChanged += EntriesOnCollectionChanged;

        _menuVisibilitySubscription = Observable.EveryValueChanged(this, x => x.IsVisible)
            .SubscribeOn(_mainWindow.FrameProvider).Subscribe(isVisible =>
            {
                if (isVisible)
                {
                    _mainWindow.IsDialogOpen = true;
                    vm.TopTitlebarViewModel.DropDownMenu.CloseMenus(Unit.Default);
                    MaxHeight = _mainWindow.UIHelper.GetMainView.Bounds.Height - 1;
                    vm.FileHistory.UpdateHistory();
                }
                else
                {
                    _mainWindow.IsDialogOpen = false;
                    // Reset it, so that it opens in default state the next time it opens
                    vm.TopTitlebarViewModel.DropDownMenu.CloseToDefault();
                }
            }, DebugHelper.LogError(nameof(DropDownMenu), nameof(_menuVisibilitySubscription)));
    }

    private void SlideShow120SecOnClick(object? sender, RoutedEventArgs e)
    {
        CloseDropDownMenu();

        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        _ = Slideshow.StartSlideshow(core.MainWindows.ActiveWindow.CurrentValue, 120000);
    }

    private void SlideShow90SecOnClick(object? sender, RoutedEventArgs e)
    {
        CloseDropDownMenu();

        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        _ = Slideshow.StartSlideshow(core.MainWindows.ActiveWindow.CurrentValue, 90000);
    }

    private void SlideShow60SecOnClick(object? sender, RoutedEventArgs e)
    {
        CloseDropDownMenu();

        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        _ = Slideshow.StartSlideshow(core.MainWindows.ActiveWindow.CurrentValue, 60000);
    }

    private void SlideShow30SecOnClick(object? sender, RoutedEventArgs e)
    {
        CloseDropDownMenu();

        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        _ = Slideshow.StartSlideshow(core.MainWindows.ActiveWindow.CurrentValue, 30000);
    }

    private void SlideShow20SecOnClick(object? sender, RoutedEventArgs e)
    {
        CloseDropDownMenu();

        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        _ = Slideshow.StartSlideshow(core.MainWindows.ActiveWindow.CurrentValue, 20000);
    }

    private void SlideShow10SecOnClick(object? sender, RoutedEventArgs e)
    {
        CloseDropDownMenu();

        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        _ = Slideshow.StartSlideshow(core.MainWindows.ActiveWindow.CurrentValue, 10000);
    }

    private void SlideShow5SecOnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        _ = Slideshow.StartSlideshow(core.MainWindows.ActiveWindow.CurrentValue, 5000);
    }

    private void SlideShow2SecOnClick(object? sender, RoutedEventArgs e)
    {
        CloseDropDownMenu();
        
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        _ = Slideshow.StartSlideshow(core.MainWindows.ActiveWindow.CurrentValue, 2000);
    }

    private void PinnedEntriesOnCollectionChanged(in NotifyCollectionChangedEventArgs<FileHistoryEntryViewModel> e)
    {
        UpdateCollection(e, PinnedEntriesCollection);
    }
    
    private void EntriesOnCollectionChanged(in NotifyCollectionChangedEventArgs<FileHistoryEntryViewModel> e)
    {
        UpdateCollection(e, UnPinnedEntriesCollection);
    }

    private void UpdateCollection(in NotifyCollectionChangedEventArgs<FileHistoryEntryViewModel> e, ItemsControl collection)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.IsSingleItem)
                {
                    var newItem = e.NewItem;
                    Dispatcher.CurrentDispatcher.Post(() => { collection.Items.Add(newItem); },
                        DispatcherPriority.Background);
                }
                else
                {
                    foreach (var item in e.NewItems)
                    {
                        Dispatcher.CurrentDispatcher.Post(() => { collection.Items.Add(item); },
                            DispatcherPriority.Background);
                    }
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.IsSingleItem)
                {
                    var removedItem = e.OldItem;
                    Dispatcher.CurrentDispatcher.Post(() => { collection.Items.Remove(removedItem); },
                        DispatcherPriority.Background);
                }
                else
                {
                    foreach (var item in e.OldItems)
                    {
                        Dispatcher.CurrentDispatcher.Post(() => { collection.Items.Remove(item); },
                            DispatcherPriority.Background);
                    }
                }
                break;
            case NotifyCollectionChangedAction.Replace:
                break;
            case NotifyCollectionChangedAction.Move:
                break;
            case NotifyCollectionChangedAction.Reset:
                collection.Items.Clear();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        Loaded -= OnLoaded;
        _menuVisibilitySubscription?.Dispose();
        if (_mainWindow.DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        
        vm.FileHistory.PinnedEntries.CollectionChanged -= PinnedEntriesOnCollectionChanged;
        vm.FileHistory.Entries.CollectionChanged -= EntriesOnCollectionChanged;
        
        SlideShow2Sec.Click -= SlideShow2SecOnClick;
        SlideShow5Sec.Click -= SlideShow5SecOnClick;
        SlideShow10Sec.Click -= SlideShow10SecOnClick;
        SlideShow20Sec.Click -= SlideShow20SecOnClick;
        SlideShow30Sec.Click -= SlideShow30SecOnClick;
        SlideShow60Sec.Click -= SlideShow60SecOnClick;
        SlideShow90Sec.Click -= SlideShow90SecOnClick;
        SlideShow120Sec.Click -= SlideShow120SecOnClick;
        
        GC.SuppressFinalize(this);
    }

    private void Close_OnClick(object? sender, RoutedEventArgs e)
        => CloseDropDownMenu();

    private void CloseDropDownMenu()
    {
        // Trigger closing animation
        IsOpen = false;
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        // Let view model know it is closed
        vm.TopTitlebarViewModel.DropDownMenu.IsDropDownMenuVisible.Value = false;
        vm.TopTitlebarViewModel.DropDownMenu.CloseMenus(Unit.Default);
    }
}