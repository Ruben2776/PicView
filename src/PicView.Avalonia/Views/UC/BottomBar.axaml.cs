using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Interactivity;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.DragAndDrop;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.UC.PopUps;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Views.UC;

public partial class BottomBar : UserControl, IDisposable
{
    public BottomBar()
    {
        InitializeComponent();

        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        PointerPressed += OnPointerPressed;
        PointerExited += OnPointerExited;

        PreviousButton.Click += OnPreviousButtonClick;
        NextButton.Click += OnNextButtonClick;
        RotateRightButton.Click += OnRotateRightButtonClick;
        RotateLeftButton.Click += OnRotateLeftButtonClick;
        FileMenuButton.Click += OnFileMenuButtonClick;
        ZoomInButton.Click += OnZoomInButtonClick;
        ZoomOutButton.Click += OnZoomOutButtonClick;
        ResetZoomButton.Click += OnResetZoomButtonClick;
        FlipButton.Click += OnFlipButtonClick;
        SettingsMenuButton.Click += OnSettingsMenuButtonClick;

        if (Settings.Theme.GlassTheme)
        {
            ApplyGlassTheme();
        }
        else if (!Settings.Theme.Dark)
        {
            ApplyLightTheme();
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e) => MoveWindow(e);

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            return;
        }
        DragAndDropManager.RemoveDragDropView(mainWindow);
    }

    private void OnPreviousButtonClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        vm.IsNavigationButtonLeftClicked = true;
        vm.TopTitlebarViewModel.CloseDropDownMenu();
        UIHelper.SetButtonInterval((IconButton?)PreviousButton);
    }

    private void OnNextButtonClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        vm.IsNavigationButtonRightClicked = true;
        vm.TopTitlebarViewModel.CloseDropDownMenu();
        UIHelper.SetButtonInterval((IconButton?)NextButton);
    }

    private void OnRotateRightButtonClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        vm.TopTitlebarViewModel.CloseDropDownMenu();
        vm.IsBottomToolbarRightRotationClicked = true;
    }

    private void OnRotateLeftButtonClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        vm.TopTitlebarViewModel.CloseDropDownMenu();
        vm.IsBottomToolbarLeftRotationClicked = true;
    }

    private void OnFileMenuButtonClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (Bounds.Width <= SizeDefaults.BottomMenusBp)
        {
            if (TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
            {
                return;
            }

            mainWindow.AddQuickEditingDialog();
        }
        else
        {
            vm.TopTitlebarViewModel.DropDownMenu.ToggleFileMenu();
        }
    }

    private void OnZoomInButtonClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.TopTitlebarViewModel.CloseDropDownMenu();
        }
    }

    private void OnZoomOutButtonClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.TopTitlebarViewModel.CloseDropDownMenu();
        }
    }

    private void OnResetZoomButtonClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.TopTitlebarViewModel.CloseDropDownMenu();
        }
    }

    private void OnFlipButtonClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.TopTitlebarViewModel.CloseDropDownMenu();
        }
    }

    private void OnSettingsMenuButtonClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (Bounds.Width <= SizeDefaults.BottomMenusBp)
        {
            if (TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
            {
                return;
            }

            mainWindow.AddQuickSettingsDialog();
        }
        else
        {
            vm.TopTitlebarViewModel.DropDownMenu.ToggleSettingsMenu();
        }
    }
    
    private void ApplyGlassTheme()
    {
        if (!Application.Current.TryGetResource("SecondaryTextColor",
                Application.Current.RequestedThemeVariant, out var textColor))
        {
            return;
        }
        
        if (textColor is not Color color)
        {
            return;
        }
        var alphaBrush = new SolidColorBrush(Color.FromArgb(15, 100, 100, 100));
                
        MainBottomBorder.Background = Brushes.Transparent;
        MainBottomBorder.BorderThickness = new Thickness(0);

        FileMenuButton.Background = Brushes.Transparent;
        FileMenuButton.Classes.Remove("noBorderHover");
        FileMenuButton.Classes.Add("hover");

        ResetZoomButton.Background = Brushes.Transparent;
        ResetZoomButton.Classes.Remove("noBorderHover");
        ResetZoomButton.Classes.Add("hover");

        SettingsMenuButton.Background = Brushes.Transparent;
        SettingsMenuButton.Classes.Remove("noBorderHover");
        SettingsMenuButton.Classes.Add("hover");

        ZoomInButton.Background = alphaBrush;
        ZoomInButton.Classes.Remove("noBorderHover");
        ZoomInButton.Classes.Add("hover");

        ZoomOutButton.Background = alphaBrush;
        ZoomOutButton.Classes.Remove("noBorderHover");
        ZoomOutButton.Classes.Add("hover");

        NextButton.Background = alphaBrush;

        PreviousButton.Background = alphaBrush;

        RotateRightButton.Background = alphaBrush;
        RotateRightButton.Classes.Remove("noBorderHover");
        RotateRightButton.Classes.Add("hover");
                
        RotateLeftButton.Background = Brushes.Transparent;
        RotateLeftButton.Classes.Remove("noBorderHover");
        RotateLeftButton.Classes.Add("hover");

        FlipButton.Background = alphaBrush;
        FlipButton.Classes.Remove("noBorderHover");
        FlipButton.Classes.Add("hover");

        FileMenuButton.Foreground = new SolidColorBrush(color);
        RotateLeftButton.Foreground = new SolidColorBrush(color);
        ResetZoomButton.Foreground = new SolidColorBrush(color);
        SettingsMenuButton.Foreground = new SolidColorBrush(color);

        NextButton.Foreground = new SolidColorBrush(color);
        PreviousButton.Foreground = new SolidColorBrush(color);
    }

    private void ApplyLightTheme()
    {
        FileMenuButton.Classes.Remove("BottomBarHover");
        FileMenuButton.Classes.Add("AltHover");

        RotateLeftButton.Classes.Remove("BottomBarToolHover");
        RotateLeftButton.Classes.Add("AltHover");

        ResetZoomButton.Classes.Remove("BottomBarToolHover");
        ResetZoomButton.Classes.Add("AltHover");

        ZoomOutButton.Classes.Remove("BottomBarToolHover");
        ZoomOutButton.Classes.Add("AltHover");
        
        ZoomInButton.Classes.Remove("BottomBarToolHover");
        ZoomInButton.Classes.Add("AltHover");

        RotateRightButton.Classes.Remove("BottomBarToolHover");
        RotateRightButton.Classes.Add("AltHover");
        
        FlipButton.Classes.Remove("BottomBarToolHover");
        FlipButton.Classes.Add("AltHover");
        
        SettingsMenuButton.Classes.Remove("BottomBarHover");
        SettingsMenuButton.Classes.Add("AltHover");
    }

    private void MoveWindow(PointerPressedEventArgs e)
    {
        if (VisualRoot is null)
        {
            return;
        }

        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            return;
        }

        if (DataContext is MainWindowViewModel vm)
        {
            vm.TopTitlebarViewModel.CloseDropDownMenu();
        }

        if (TopLevel.GetTopLevel(this) is MainWindow mainWindow)
        {
            WindowFunctions.WindowDragBehavior(mainWindow, e);
        }
    }

    public void ResponsiveNavigationBtnSize(double width)
    {
        switch (width)
        {
            case > SizeDefaults.FullBtnBp:
                ResetZoomButton.IsVisible = RotateLeftButton.IsVisible = true;
                PreviousButton.Width = NextButton.Width = 80;
                return;
            case < SizeDefaults.FullBtnBp and > SizeDefaults.SearchResetAndRotateBtnBp:
                PreviousButton.Width = NextButton.Width = 65;
                break;
            case < SizeDefaults.SearchResetAndRotateBtnBp and > SizeDefaults.SmallNavBtnBp:
                PreviousButton.Width = NextButton.Width = 60;
                break;
            default:
                ZoomInButton.Width = ZoomOutButton.Width = FlipButton.Width = RotateRightButton.Width = 33;
                PreviousButton.Width = NextButton.Width = 50;
                break;
        }

        if (width < SizeDefaults.SearchResetAndRotateBtnBp)
        {
            ResetZoomButton.IsVisible = RotateLeftButton.IsVisible = false;
        }
        else
        {
            ZoomInButton.Width = ZoomOutButton.Width = FlipButton.Width = RotateRightButton.Width = 38;
        }
    }
    
    public void Dispose()
    {
        Loaded -= OnLoaded;
        PointerPressed -= OnPointerPressed;
        PointerExited -= OnPointerExited;

        PreviousButton.Click -= OnPreviousButtonClick;
        NextButton.Click -= OnNextButtonClick;
        RotateRightButton.Click -= OnRotateRightButtonClick;
        RotateLeftButton.Click -= OnRotateLeftButtonClick;
        FileMenuButton.Click -= OnFileMenuButtonClick;
        ZoomInButton.Click -= OnZoomInButtonClick;
        ZoomOutButton.Click -= OnZoomOutButtonClick;
        ResetZoomButton.Click -= OnResetZoomButtonClick;
        FlipButton.Click -= OnFlipButtonClick;
        SettingsMenuButton.Click -= OnSettingsMenuButtonClick;

        GC.SuppressFinalize(this);
    }
}