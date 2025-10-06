using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using PicView.Avalonia.Functions;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Sizing;
using R3;

namespace PicView.Avalonia.ViewModels;

public class MainWindowViewModel : IDisposable
{
    public TopTitlebarViewModel? TopTitlebarViewModel { get; set; }
    
    public bool IsNavigationButtonLeftClicked { get; set; }
    public bool IsNavigationButtonRightClicked { get; set; }
    
    public bool IsClickArrowLeftClicked { get; set; }
    public bool IsClickArrowRightClicked { get; set; }

    public bool IsBottomToolbarRotationClicked { get; set; }

    public BindableReactiveProperty<Brush?> ImageBackground { get; } = new();

    public BindableReactiveProperty<Brush?> ConstrainedImageBackground { get; } = new();

    public BindableReactiveProperty<Thickness> RightControlOffSetMargin { get; } = new();

    public BindableReactiveProperty<Thickness> TopScreenMargin { get; } = new();

    public BindableReactiveProperty<Thickness> BottomScreenMargin { get; } = new();

    public BindableReactiveProperty<CornerRadius> BottomCornerRadius { get; } = new();

    public BindableReactiveProperty<int> BackgroundChoice { get; } = new();

    public BindableReactiveProperty<double> WindowMinWidth { get; } = new(SizeDefaults.WindowMinSize);

    public BindableReactiveProperty<double> SecondaryWindowMinWidth { get; } =
        new(SizeDefaults.SecondaryWindowMinWidth);
    public BindableReactiveProperty<double> WindowMinHeight { get; } = new(SizeDefaults.WindowMinSize);

    public BindableReactiveProperty<double> TitlebarHeight { get; } = new();

    public BindableReactiveProperty<double> BottombarHeight { get; } = new();

    public BindableReactiveProperty<SizeToContent> SizeToContent { get; } = new();

    public BindableReactiveProperty<ScrollBarVisibility> ToggleScrollBarVisibility { get; } = new();

    public BindableReactiveProperty<bool> CanResize { get; } = new();

    public BindableReactiveProperty<UserControl?> CurrentView { get; } = new();

    public BindableReactiveProperty<bool> IsFileMenuVisible { get; } = new();

    public BindableReactiveProperty<bool> IsImageMenuVisible { get; } = new();

    public BindableReactiveProperty<bool> IsSettingsMenuVisible { get; } = new();

    public BindableReactiveProperty<bool> IsToolsMenuVisible { get; } = new();

    public BindableReactiveProperty<double> TitleMaxWidth { get; } = new();

    public BindableReactiveProperty<bool> IsFullscreen { get; } = new();

    public BindableReactiveProperty<bool> IsMaximized { get; } = new();

    public BindableReactiveProperty<bool> ShouldRestore { get; } = new();

    public BindableReactiveProperty<bool> ShouldMaximizeBeShown { get; } = new(true);

    public BindableReactiveProperty<bool> IsLoadingIndicatorShown { get; } = new();

    public BindableReactiveProperty<bool> IsUIShown { get; } = new();
    public BindableReactiveProperty<bool> IsTopToolbarShown { get; } = new();

    public BindableReactiveProperty<bool> IsBottomToolbarShown { get; } = new();

    public BindableReactiveProperty<bool> IsEditableTitlebarOpen { get; } = new();

    public BindableReactiveProperty<IImage?> ChangeCtrlZoomImage { get; } = new();

    public ReactiveCommand ExitCommand { get; } = new(Close);

    public ReactiveCommand MaximizeCommand { get; } = new(async (_, _) => { await FunctionsMapper.Maximize(); });

    public ReactiveCommand MinimizeCommand { get; } = new(async (_, _) => { await WindowFunctions.Minimize(); });

    public ReactiveCommand RestoreCommand { get; } = new(async (_, _) => { await FunctionsMapper.Restore(); });

    public ReactiveCommand ToggleFullscreenCommand { get; } = new(async (_, _) =>
    {
        await FunctionsMapper.ToggleFullscreen();
    });

    public void Dispose()
    {
        Disposable.Dispose(ImageBackground,
            ConstrainedImageBackground,
            RightControlOffSetMargin,
            TopScreenMargin,
            BottomScreenMargin,
            BottomCornerRadius,
            BackgroundChoice,
            WindowMinWidth,
            WindowMinHeight,
            TitlebarHeight,
            BottombarHeight,
            SizeToContent,
            CanResize,
            CurrentView,
            TitleMaxWidth,
            IsLoadingIndicatorShown,
            IsUIShown,
            IsTopToolbarShown,
            IsBottomToolbarShown,
            IsEditableTitlebarOpen);
    }

    public void LayoutButtonSubscription()
    {
        Observable.EveryValueChanged(this, x => x.IsMaximized.CurrentValue, UIHelper.GetFrameProvider)
            .Subscribe(_ => SetButtonValues());
        Observable.EveryValueChanged(this, x => x.IsFullscreen.CurrentValue, UIHelper.GetFrameProvider)
            .Subscribe(isFullscreen =>
            {
                SetButtonValues();
                if (UIHelper.GetMainView.DataContext is MainViewModel vm)
                {
                    vm.HoverbarViewModel.IsHoverbarVisible.Value = isFullscreen && Settings.WindowProperties.Fullscreen;
                }
            });
    }

    public static void HoverBarSubscription()
    {
        if (UIHelper.GetMainView.DataContext is not MainViewModel vm)
        {
            return;
        }

        Observable.EveryValueChanged(vm.MainWindow.CurrentView, control => control.Value)
            .Subscribe(control =>
            {
                if (control is ImageViewer)
                {
                    if (Settings.WindowProperties.Fullscreen)
                    {
                        vm.HoverbarViewModel.IsHoverbarVisible.Value = Settings.UIProperties.ShowAltInterfaceButtons;
                    }
                    else if (!Settings.UIProperties.ShowBottomNavBar && Settings.UIProperties.ShowAltInterfaceButtons)
                    {
                        vm.HoverbarViewModel.IsHoverbarVisible.Value = true;
                    }
                    else
                    {
                        vm.HoverbarViewModel.IsHoverbarVisible.Value = false;
                    }
                }
                else
                {
                    vm.HoverbarViewModel.IsHoverbarVisible.Value = false;
                }
            });
    }

    private static void Close(Unit unit) => DialogManager.Close();

    public static (int WindowMinWidth, int WindowMinHeight) GetAndSetWindowMinSize(MainViewModel vm)
    {
        if (Settings.UIProperties.ShowBottomNavBar)
        {
            vm.MainWindow.WindowMinWidth.Value = SizeDefaults.WindowMinSize;
            vm.MainWindow.WindowMinHeight.Value = SizeDefaults.WindowMinSize;
            return (SizeDefaults.WindowMinSize, SizeDefaults.WindowMinSize);
        }
        
        const int minHeight = 100;
        vm.MainWindow.WindowMinWidth.Value = vm.PlatformWindowService.CombinedTitleButtonsWidth;
        vm.MainWindow.WindowMinHeight.Value = minHeight;
        return (vm.PlatformWindowService.CombinedTitleButtonsWidth, minHeight);
    }


    private void SetButtonValues()
    {
        ShouldRestore.Value = IsFullscreen.CurrentValue || IsMaximized.CurrentValue;
        ShouldMaximizeBeShown.Value = !IsFullscreen.CurrentValue && !IsMaximized.CurrentValue;
    }

    #region Menus

    public ReactiveCommand CloseMenuCommand { get; } = new(CloseMenus);

    public ReactiveCommand ToggleFileMenuCommand { get; } = new(ToggleFileMenu);
    public ReactiveCommand ToggleImageMenuCommand { get; } = new(ToggleImageMenu);
    public ReactiveCommand ToggleSettingsMenuCommand { get; } = new(ToggleSettingsMenu);
    public ReactiveCommand ToggleToolsMenuCommand { get; } = new(ToggleToolsMenu);

    private static void CloseMenus(Unit unit) =>
        MenuManager.CloseMenus(UIHelper.GetMainView.DataContext as MainViewModel);

    private static void ToggleFileMenu(Unit unit) =>
        MenuManager.ToggleFileMenu(UIHelper.GetMainView.DataContext as MainViewModel);

    private static void ToggleImageMenu(Unit unit) =>
        MenuManager.ToggleImageMenu(UIHelper.GetMainView.DataContext as MainViewModel);

    private static void ToggleSettingsMenu(Unit unit) =>
        MenuManager.ToggleSettingsMenu(UIHelper.GetMainView.DataContext as MainViewModel);

    private static void ToggleToolsMenu(Unit unit) =>
        MenuManager.ToggleToolsMenu(UIHelper.GetMainView.DataContext as MainViewModel);

    #endregion Menus
}