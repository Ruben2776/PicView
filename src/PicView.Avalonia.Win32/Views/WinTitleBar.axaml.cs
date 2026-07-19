using Avalonia;
using Avalonia.Controls;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.DebugTools;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Win32.Views;

public partial class WinTitleBar : MainTitleBar
{
    public WinTitleBar()
    {
        InitializeComponent();
        SharedDropDownMenuButton = DropDownMenuButton;
        SharedSearchButton = SearchButton;
        
        Loaded += (_, _) =>
        {
            if (Settings.Theme.GlassTheme)
            {
                ApplyGlassThemeStyles();
            }
            else if (!Settings.Theme.Dark)
            {
                ApplyLightThemeStyles();
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
        GlassThemeHelper.ApplyTransparentStyle(DropDownMenuButton);
        GlassThemeHelper.ApplyTransparentStyle(MenuButton);
        GlassThemeHelper.ApplyTransparentStyle(MainMenu);

        SetSecondaryForeground();
    }
    
    private void SetSecondaryForeground()
    {
        var secondaryTextColor = UIHelper.GetBrush("SecondaryTextColor");
        EditableTitlebar.Foreground = secondaryTextColor;
        CloseButton.Foreground = secondaryTextColor;
        MinimizeButton.Foreground = secondaryTextColor;
        RestoreButton.Foreground = secondaryTextColor;
        DropDownMenuButton.Foreground = secondaryTextColor;
        MenuButton.Foreground = secondaryTextColor;
    }
    
    private void ApplyLightThemeStyles()
    {
        UIHelper.SwitchHoverBorderClass(MenuButton);
        UIHelper.SwitchHoverBorderClass(SearchButton);
        UIHelper.SwitchHoverBorderClass(DropDownMenuButton);
        UIHelper.SwitchHoverBorderClass(CreateTabButton);
    }
    
    private void InitializeEventHandlers()
    {
        if (DataContext is not MainWindowViewModel vm || TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            return;
        }
        
        MainMenu.Closed += (_, _) => { CloseMenu(); };
        
        Observable.EveryValueChanged(vm.TopTitlebarViewModel.IsMainMenuVisible, x => x.Value,
                mainWindow.FrameProvider)
            .Skip(1)
            .Subscribe( isVisible =>
            {
                if (isVisible)
                {
                    // Overflow buttons if the window is too small
                    if (Bounds.Width - SearchButton.Bounds.Width - CreateTabButton.Bounds.Width < SizeDefaults.MainTitleDropDownBtnBp)
                    {
                        OpenTruncatedMenu(vm);
                    }
                    else
                    {
                        OpenRegularSizedMenu(vm);
                    }
                }
                else
                {
                    ClosedMenu(vm);
                }
            }, DebugHelper.LogError(nameof(WinTitleBar), nameof(InitializeEventHandlers)))
            .AddTo(mainWindow.Disposables);
    }

    private void OpenTruncatedMenu(MainWindowViewModel vm)
    {
        OpenMenu();
        vm.TopTitlebarViewModel.IsBtnPanelVisible.Value = false;
        LogoBorder.IsVisible = false;
        CreateTabButton.IsVisible = false;
        
        const int menuItemsCount = 7;
        vm.TopTitlebarViewModel.MaxItemWidth.Value = Bounds.Width / menuItemsCount;
        
        var truncatedPadding = new Thickness(2,0,2,0);
        FileMenuItem.Padding = truncatedPadding;
        EditMenuItem.Padding = truncatedPadding;
        ViewMenuItem.Padding = truncatedPadding;
        ImageMenuItem.Padding = truncatedPadding;
        NavigateMenuItem.Padding = truncatedPadding;
        SettingsMenuItem.Padding = truncatedPadding;
        HelpMenuItem.Padding = truncatedPadding;
    }
    
    private void OpenRegularSizedMenu(MainWindowViewModel vm)
    {
        OpenMenu();
        vm.TopTitlebarViewModel.IsBtnPanelVisible.Value = true;
        LogoBorder.IsVisible = true;
        vm.TopTitlebarViewModel.MaxItemWidth.Value = double.NaN;
        CreateTabButton.IsVisible = false;
                
        var regularPadding = new Thickness(8);
        FileMenuItem.Padding = regularPadding;
        EditMenuItem.Padding = regularPadding;
        ViewMenuItem.Padding = regularPadding;
        ImageMenuItem.Padding = regularPadding;
        NavigateMenuItem.Padding = regularPadding;
        SettingsMenuItem.Padding = regularPadding;
        HelpMenuItem.Padding = regularPadding;
    }
    
    private void OpenMenu()
    {
        MainMenu.Open();
        FileMenuItem.Open();
    }
    
    private void ClosedMenu(MainWindowViewModel vm)
    {
        MainMenu.Close();
        vm.TopTitlebarViewModel.IsBtnPanelVisible.Value = true;
        LogoBorder.IsVisible = true;
        vm.TopTitlebarViewModel.MaxItemWidth.Value = double.NaN;
        DropDownMenuButton.IsVisible = Bounds.Width > SizeDefaults.MainTitleDropDownBtnBp;
        CreateTabButton.IsVisible = true;
    }

    private void CloseMenu()
    {
        MainMenu.Close();

        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        vm.TopTitlebarViewModel.CloseMenu();
    }
}