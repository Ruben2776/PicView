using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ObservableCollections;
using PicView.Avalonia.Crop;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Navigation;
using PicView.Core.DebugTools;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.Menu;

public partial class DropDownMenu : AnimatedMenu
{
    private DisposableBag _disposables;
    private readonly MainWindow _mainWindow;

    public DropDownMenu(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        if (mainWindow.DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        vm.FileHistory ??= new FileHistoryViewModel(vm);
        vm.Favorites ??= new FavoritesViewModel(vm);
        vm.Favorites.AddFavoritesDialogCommand.Subscribe(
        _ =>
        {
            _mainWindow.AddFavoriteDialog();
        }, DebugHelper.LogError(nameof(DropDownMenu), nameof(vm.Favorites.AddFavoritesDialogCommand)))
        .AddTo(ref _disposables);
        vm.Favorites.EditFavoritesDialogCommand.Subscribe(entry =>
        {
            _mainWindow.AddFavoriteDialog(entry.FilePath, entry.HotKey);
        }, DebugHelper.LogError(nameof(DropDownMenu), nameof(vm.Favorites.EditFavoritesDialogCommand)))
        .AddTo(ref _disposables);
        DataContext = vm;
        InitializeComponent();
        Loaded += OnLoaded;
        if (!Settings.Theme.Dark)
        {
            ApplyLightTheme();
        }
    }
    
    private void ApplyLightTheme()
    {
        AutoFitButton.Classes.Remove("hover");
        AutoFitButton.Classes.Add("AltHover");
        
        TopMostButton.Classes.Remove("hover");
        TopMostButton.Classes.Add("AltHover");
        
        LoopingButton.Classes.Remove("hover");
        LoopingButton.Classes.Add("AltHover");

        ScrollButton.Classes.Remove("hover");
        ScrollButton.Classes.Add("AltHover");
        
        ZoomToFitButton.Classes.Remove("hover");
        ZoomToFitButton.Classes.Add("AltHover");
        
        SideBySideButton.Classes.Remove("hover");
        SideBySideButton.Classes.Add("AltHover");
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
        if (vm.FileHistory.PinnedEntries.Count > 0)
        {
            PinnedEntriesCollection.Items.Clear();
            foreach (var item in vm.FileHistory.PinnedEntries)
            {
                PinnedEntriesCollection.Items.Add(item);
            }
        }

        vm.FileHistory.Entries.CollectionChanged += EntriesOnCollectionChanged;
        if (vm.FileHistory.Entries.Count > 0)
        {
            UnPinnedEntriesCollection.Items.Clear();
            foreach (var item in vm.FileHistory.Entries)
            {
                UnPinnedEntriesCollection.Items.Add(item);
            }
        }

        if (vm.Favorites is not null)
        {
            vm.Favorites.Entries.CollectionChanged += FavoritesOnCollectionChanged;
            if (vm.Favorites.Entries.Count > 0)
            {
                FavoritesCollection.Items.Clear();
                foreach (var item in vm.Favorites.Entries)
                {
                    FavoritesCollection.Items.Add(item);
                }
            }
        }

        Observable.EveryValueChanged(this, x => x.IsVisible)
            .Skip(1)
            .SubscribeOn(_mainWindow.FrameProvider).Subscribe(isVisible =>
            {
                if (isVisible)
                {
                    _mainWindow.IsDialogOpen = true;
                    vm.TopTitlebarViewModel.DropDownMenu.CloseMenus(Unit.Default);
                    MaxHeight = _mainWindow.UIHelper.GetMainView.Bounds.Height - 1;
                    vm.WindowTabs.ActiveTab.CurrentValue.ShouldCropBeEnabled.Value =
                        CropManager.SetIfCropEnabled(_mainWindow);
                    vm.FileHistory.UpdateHistory();
                    vm.Favorites.UpdateFavorites();
                }
                else
                {
                    _mainWindow.IsDialogOpen = false;
                    // Reset it, so that it opens in default state the next time it opens
                    vm.TopTitlebarViewModel.DropDownMenu.CloseToDefault();
                }
            }, DebugHelper.LogError(nameof(DropDownMenu), nameof(IsVisible)))
            .AddTo(ref _disposables);
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

    private void FavoritesOnCollectionChanged(in NotifyCollectionChangedEventArgs<FavoritesEntryViewModel> e)
    {
        UpdateCollection(e, FavoritesCollection);
    }

    private static void UpdateCollection<T>(in NotifyCollectionChangedEventArgs<T> e, ItemsControl collection)
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
                Dispatcher.CurrentDispatcher.Post(() => { collection.Items.Clear(); },
                    DispatcherPriority.Background);
                break;
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        Loaded -= OnLoaded;
        _disposables.Dispose();
        if (_mainWindow.DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (vm.Favorites is not null)
        {
            vm.Favorites.Entries.CollectionChanged -= FavoritesOnCollectionChanged;
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