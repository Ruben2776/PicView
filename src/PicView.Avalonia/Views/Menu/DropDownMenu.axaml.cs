using System.Collections.ObjectModel;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ObservableCollections;
using PicView.Avalonia.Crop;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.Conversion;
using PicView.Core.DebugTools;
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
        
        vm.Favorites.AddFavoritesDialogCommand.Subscribe(_ =>
            {
                _mainWindow.AddFavoriteDialog();
            }, DebugHelper.LogError(nameof(DropDownMenu), nameof(vm.Favorites.AddFavoritesDialogCommand)))
            .AddTo(ref _disposables);
        vm.Favorites.EditFavoritesDialogCommand.Subscribe(entry =>
            {
                _mainWindow.AddFavoriteDialog(entry.FilePath);
            }, DebugHelper.LogError(nameof(DropDownMenu), nameof(vm.Favorites.EditFavoritesDialogCommand)))
            .AddTo(ref _disposables);
        
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

    private ObservableCollection<T> Mirror<T>(ObservableList<T> source) =>
        new DeferredListMirror<T>(source, PostBelowRendering).AddTo(ref _disposables).Items;

    private static void PostBelowRendering(Action action) =>
        Dispatcher.UIThread.Post(action, DispatcherPriority.Background);

    public override void Dispose()
    {
        base.Dispose();
        Loaded -= OnLoaded;
        _disposables.Dispose();
        
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