using System.Collections.ObjectModel;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ObservableCollections;
using PicView.Avalonia.UI;
using PicView.Avalonia.Crop;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Navigation;
using PicView.Core.Conversion;
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
        
        DataContext = vm;
        vm.FileHistory ??= new FileHistoryViewModel(vm);
        vm.Favorites ??= new FavoritesViewModel(vm);
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
        Dispatcher.UIThread.Post(Initialize, DispatcherPriority.Background);
    }

    private void Initialize()
    {
        if (_mainWindow.DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        
        vm.Favorites.AddFavoritesDialogCommand.Subscribe(
                _ =>
                {
                    _mainWindow.AddFavoriteDialog();
                }, DebugHelper.LogError(nameof(DropDownMenu), nameof(vm.Favorites.AddFavoritesDialogCommand)))
            .AddTo(ref _disposables);
        vm.Favorites.EditFavoritesDialogCommand.Subscribe(entry =>
            {
                _mainWindow.AddFavoriteDialog(entry.FilePath);
            }, DebugHelper.LogError(nameof(DropDownMenu), nameof(vm.Favorites.EditFavoritesDialogCommand)))
            .AddTo(ref _disposables);
        
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

        // The lists are moved, inserted into and removed from in place, so the views follow them change by change and
        // keep their layout and scroll position. Each change is posted below rendering, so the first fill builds its
        // rows between frames instead of stalling the menu as it opens
        PinnedEntriesCollection.ItemsSource = Mirror(vm.FileHistory.PinnedEntries);
        UnPinnedEntriesCollection.ItemsSource = Mirror(vm.FileHistory.Entries);
        FavoritesCollection.ItemsSource = Mirror(vm.Favorites.Entries);

        Observable.EveryValueChanged(this, x => x.IsVisible)
            .Skip(1)
            .SubscribeOn(_mainWindow.FrameProvider).Subscribe(isVisible =>
            {
                if (isVisible)
                {
                    var tab = vm.WindowTabs.ActiveTab.CurrentValue;
                    
                    vm.TopTitlebarViewModel.DropDownMenu.CloseMenus(Unit.Default);
                    MaxHeight = _mainWindow.UIHelper.GetMainView.Bounds.Height - 1;
                    tab.ShouldCropBeEnabled.Value = CropManager.SetIfCropEnabled(_mainWindow);
                    vm.FileHistory.UpdateHistory();
                    vm.Favorites.ShowFavorites();
                    
                    tab.ShouldPrintBeEnabled.Value = ConversionHelper.DetermineIfPrintMenuItemShouldBeEnabled(tab);
                }
                else
                {
                    // Reset it, so that it opens in default state the next time it opens
                    vm.TopTitlebarViewModel.DropDownMenu.CloseToDefault();
                }
            }, DebugHelper.LogError(nameof(DropDownMenu), nameof(IsVisible)))
            .AddTo(ref _disposables);
    }

    private void SlideShow120SecOnClick(object? sender, RoutedEventArgs e)
    {
        CloseDropDownMenu();
        _ = Slideshow.StartSlideshow(_mainWindow, 120000);
    }

    private void SlideShow90SecOnClick(object? sender, RoutedEventArgs e)
    {
        CloseDropDownMenu();
        _ = Slideshow.StartSlideshow(_mainWindow, 90000);
    }

    private void SlideShow60SecOnClick(object? sender, RoutedEventArgs e)
    {
        CloseDropDownMenu();
        _ = Slideshow.StartSlideshow(_mainWindow, 60000);
    }

    private void SlideShow30SecOnClick(object? sender, RoutedEventArgs e)
    {
        CloseDropDownMenu();
        _ = Slideshow.StartSlideshow(_mainWindow, 30000);
    }

    private void SlideShow20SecOnClick(object? sender, RoutedEventArgs e)
    {
        CloseDropDownMenu();
        _ = Slideshow.StartSlideshow(_mainWindow, 20000);
    }

    private void SlideShow10SecOnClick(object? sender, RoutedEventArgs e)
    {
        CloseDropDownMenu();
        _ = Slideshow.StartSlideshow(_mainWindow, 10000);
    }

    private void SlideShow5SecOnClick(object? sender, RoutedEventArgs e)
    {
        CloseDropDownMenu();
        _ = Slideshow.StartSlideshow(_mainWindow, 5000);
    }

    private void SlideShow2SecOnClick(object? sender, RoutedEventArgs e)
    {
        CloseDropDownMenu();
        _ = Slideshow.StartSlideshow(_mainWindow, 2000);
    }

    private ObservableCollection<T> Mirror<T>(ObservableList<T> source) =>
        new DeferredListMirror<T>(source, PostBelowRendering).AddTo(ref _disposables).Items;

    private static void PostBelowRendering(Action action) =>
        Dispatcher.UIThread.Post(action, DispatcherPriority.Background);

    public override void Dispose()
    {
        base.Dispose();
        Loaded -= OnLoaded;
        _disposables.Dispose();

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