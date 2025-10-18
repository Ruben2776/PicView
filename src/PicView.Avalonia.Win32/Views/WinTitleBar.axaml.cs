using Avalonia.Controls;
using Avalonia.Input;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.DragAndDrop;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.WindowBehavior;
using R3;

namespace PicView.Avalonia.Win32.Views;

public partial class WinTitleBar : UserControl
{
    public WinTitleBar()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            if (Settings.Theme.GlassTheme)
            {
                ApplyGlassThemeStyles();
            }

            InitializeEventHandlers();
        };
    }

    // Extract method: centralize glass theme styling to remove duplication
    private void ApplyGlassThemeStyles()
    {
        GlassThemeHelper.ApplyTransparentStyle(TopWindowBorder);
        GlassThemeHelper.ApplyTransparentStyle(LogoBorder);
        GlassThemeHelper.ApplyTransparentStyle(EditableTitlebar);
        GlassThemeHelper.ApplyTransparentStyle(CloseButton);
        GlassThemeHelper.ApplyTransparentStyle(MinimizeButton);
        GlassThemeHelper.ApplyTransparentStyle(RestoreButton);
        GlassThemeHelper.ApplyTransparentStyle(FullscreenButton);
        GlassThemeHelper.ApplyTransparentStyle(GalleryButton);
        GlassThemeHelper.ApplyTransparentStyle(MenuButton);
        GlassThemeHelper.ApplyTransparentStyle(MainMenu);

        var glassForeground = UIHelper.GetBrush("SecondaryTextColor");
        EditableTitlebar.Foreground = glassForeground;
        CloseButton.Foreground = glassForeground;
        MinimizeButton.Foreground = glassForeground;
        RestoreButton.Foreground = glassForeground;
        GalleryButton.Foreground = glassForeground;
        MenuButton.Foreground = glassForeground;
    }
    
    private void InitializeEventHandlers()
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        
        PointerPressed += (_, e) => TryDragWindow(e);
        PointerExited += (_, _) => { DragAndDropHelper.RemoveDragDropView(); };
        MainMenu.Closed += (_, _) => { CloseMenu(); };

        Observable.EveryValueChanged(vm.MainWindow.TopTitlebarViewModel.IsMainMenuVisible, x => x.Value,
                UIHelper.GetFrameProvider)
            .Subscribe(isVisible =>
            {
                if (isVisible)
                {
                    MainMenu.Open();
                    FileMenuItem.Open();

                    // Overflow buttons if the window is too small
                    if (vm.MainWindow.TitleMaxWidth.CurrentValue <
                        vm.PlatformWindowService.CombinedTitleButtonsWidth)
                    {
                        vm.MainWindow.TopTitlebarViewModel.IsBtnPanelVisible.Value = false;
                    }
                    else
                    {
                        vm.MainWindow.TopTitlebarViewModel.IsBtnPanelVisible.Value = true;
                    }
                }
                else
                {
                    MainMenu.Close();
                    vm.MainWindow.TopTitlebarViewModel.IsBtnPanelVisible.Value = true;
                }
            });
    }

    private void CloseMenu()
    {
        MainMenu.Close();

        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        vm.MainWindow.TopTitlebarViewModel.CloseMenu();
    }

    private void TryDragWindow(PointerPressedEventArgs e)
    {
        if (VisualRoot is null || DataContext is not MainViewModel vm)
        {
            return;
        }

        if (vm.MainWindow.IsEditableTitlebarOpen.Value || MainMenu.IsOpen)
        {
            return;
        }

        WindowFunctions.WindowDragAndDoubleClickBehavior((Window)VisualRoot, e, vm.PlatformWindowService);
    }
}