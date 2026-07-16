using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.UC.PopUps;
using PicView.Core.DebugTools;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.UC;

public partial class HoverBar : UserControl, IDisposable
{
    private DisposableBag _disposables;
    public HoverBar()
    {
        InitializeComponent();
        
        if (!Settings.Theme.Dark)
        {
            ChangeHoverClasses();
        }
        
        Loaded += OnLoaded;

    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            return;
        }
        
        AddHandler(PointerPressedEvent, ManagePointerPressed, RoutingStrategies.Direct | RoutingStrategies.Tunnel);
        mainWindow.UIHelper.GetMainView.SizeChanged += (_, args) => ApplyResponsiveResize(args.NewSize.Width);
        ApplyResponsiveResize(Bounds.Width);

        if (Settings.Theme.GlassTheme)
        {
            GlassThemeUpdates();
        }

        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        
        _disposables.Add(new HoverFadeButtonHandler(this, BottomBorder));

        Observable.FromEventHandler<RoutedEventArgs>(h => NextButton.Click += h,
                h => NextButton.Click -= h)
            .SubscribeAwait(async (_, c) =>
            {
                core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverNavigationButtonNextClicked = true;
                await core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.NextFile();
            }, static result =>
            {
#if DEBUG
                if (result is { IsFailure: true, Exception: not null })
                {
                    DebugHelper.LogDebug(nameof(HoverBar), nameof(OnLoaded), result.Exception);
                }
#endif
            })
            .AddTo(ref _disposables);
        Observable.FromEventHandler<RoutedEventArgs>(h => PreviousButton.Click += h,
                h => PreviousButton.Click -= h)
            .SubscribeAwait(async (_, c) =>
            {
                core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverNavigationButtonPreviousClicked = true;
                await core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.PrevFile();
            }, static result =>
            {
#if DEBUG
                if (result is { IsFailure: true, Exception: not null })
                {
                    DebugHelper.LogDebug(nameof(HoverBar), nameof(OnLoaded), result.Exception);
                }
#endif
            })
            .AddTo(ref _disposables);

        Debug.Assert(Settings.Gallery is not null);
        Observable.EveryValueChanged(Settings.Gallery, x => x.IsGalleryDocked)
            .Skip(1)
            .Subscribe(_ =>
            {
                ApplyResponsiveResize(Bounds.Width);
            }, static result =>
            {
#if DEBUG
                if (result is { IsFailure: true, Exception: not null })
                {
                    DebugHelper.LogDebug(nameof(HoverBar), nameof(OnLoaded), result.Exception);
                }
#endif
            })
            .AddTo(ref _disposables);
    }

    #region Theming

    private void ChangeHoverClasses()
    {
        FileMenuButton.Classes.Remove("noBorderHover");
        FileMenuButton.Classes.Add("hover");

        ZoomOutMenuButton.Classes.Remove("noBorderHover");
        ZoomOutMenuButton.Classes.Add("hover");

        ZoomInMenuButton.Classes.Remove("noBorderHover");
        ZoomInMenuButton.Classes.Add("hover");

        RotateLeftButton.Classes.Remove("noBorderHover");
        RotateLeftButton.Classes.Add("hover");

        RotateRightButton.Classes.Remove("noBorderHover");
        RotateRightButton.Classes.Add("hover");

        FlipButton.Classes.Remove("noBorderHover");
        FlipButton.Classes.Add("hover");

        ImageMenuButton.Classes.Remove("noBorderHover");
        ImageMenuButton.Classes.Add("hover");

        SettingsMenuButton.Classes.Remove("noBorderHover");
        SettingsMenuButton.Classes.Add("hover");
    }

    private void GlassThemeUpdates()
    {
        var brush = new SolidColorBrush(Color.Parse("#D1333333"));
        NextButton.Background = brush;
        PreviousButton.Background = brush;

        var noThickness = new Thickness(0);
        FileMenuButton.BorderThickness = noThickness;
        ZoomOutMenuButton.BorderThickness = noThickness;
        ZoomInMenuButton.BorderThickness = noThickness;
        RotateLeftButton.BorderThickness = noThickness;
        RotateRightButton.BorderThickness = noThickness;
        FlipButton.BorderThickness = noThickness;
        ImageMenuButton.BorderThickness = noThickness;
        SettingsMenuButton.BorderThickness = noThickness;
        NextButton.BorderThickness = noThickness;
        PreviousButton.BorderThickness = noThickness;
    }
    
    #endregion
    
    private void ApplyResponsiveResize(double width)
    {
        const int firstBreakpoint = 475;
        const int secondBreakpoint = 550;
        const int thirdBreakpoint = 800;

        switch (width)
        {
            case < SizeDefaults.SecondaryWindowMinWidth:
                // Too small to fit
                IsVisible = false;
                break;
            case <= firstBreakpoint:
                ApplyLayout(
                    70,
                    false,
                    false);
                break;

            case <= secondBreakpoint:
                ApplyLayout(
                    75,
                    false,
                    false);
                break;

            case < thirdBreakpoint:
                ApplyLayout(
                    72,
                    false,
                    true);
                break;

            default:
                ApplyLayout(
                    75,
                    true,
                    true);
                break;
        }
    }

    private void ApplyLayout(double buttonWidth, bool showRotateLeft, bool showAdvancedButtons)
    {
        NextButton.Width = PreviousButton.Width = buttonWidth;
        RotateLeftButton.IsVisible = showRotateLeft;
        RotateRightButton.IsVisible =
            FlipButton.IsVisible =
                ZoomInMenuButton.IsVisible =
                    ZoomOutMenuButton.IsVisible = showAdvancedButtons;

        IsVisible = true;
        
        if ( TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            return;
        }

        // Make sure hover bar is above the bottom gallery if needed
        var newHeight = Settings.Gallery.IsGalleryDocked ? 50 : 160;
        Height = mainWindow.UIHelper.GetMainView.Bounds.Height > SizeDefaults.WindowMinSize ? newHeight : double.NaN;
    }


    private async Task ManagePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        
        var props = e.Properties;
        
        if (NextButton.IsPointerOver)
        {
            if (props.IsRightButtonPressed)
            {
                ShowNavigationDialog();
            }
            else if (props.IsLeftButtonPressed)
            {
                UIHelper.SetButtonInterval(NextButton);
            }
        }
        else if (PreviousButton.IsPointerOver)
        {
            if (props.IsRightButtonPressed)
            {
                ShowNavigationDialog();
            }
            else if (props.IsLeftButtonPressed)
            {
                UIHelper.SetButtonInterval(PreviousButton);
            }
        }
        else if (SettingsMenuButton.IsPointerOver)
        {
            if (props.IsRightButtonPressed)
            {
                ShowQuickSettingsDialog();
            }
            else if (props.IsLeftButtonPressed)
            {
                await core.MainWindows.ActiveWindow.CurrentValue.Mapper.SettingsWindow();
            }
        }
        else if (ImageMenuButton.IsPointerOver)
        {
            if (props.IsRightButtonPressed || props.IsLeftButtonPressed)
            {
                ShowQuickEditingDialog();
            }
        }
        else if (RotateLeftButton.IsPointerOver)
        {
            if (props.IsLeftButtonPressed)
            {
                core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverRotateLeftClicked = true;
            }
        }
        else if (RotateRightButton.IsPointerOver)
        {
            if (props.IsLeftButtonPressed)
            {
                core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverRotateRightClicked = true;
            }
        }
        else if (ProgressBar.IsPointerOver)
        {
            if (props.IsRightButtonPressed)
            {
                ShowSearchDialog();

                // Wait for animation to finish to properly close tooltip
                await Task.Delay(TimeSpan.FromSeconds(0.3));
                Dispatcher.UIThread.Post(() => { ToolTip.SetIsOpen(ProgressBar, false); },
                    DispatcherPriority.Background);
            }
        }
        else
        {
            if (props.IsRightButtonPressed)
            {
                if (TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
                {
                    return;
                }
                UIHelper.ShowMainContextMenu(mainWindow);
            }
        }
    }

    private void ShowNavigationDialog()
    {
        if (TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            return;
        }
        mainWindow.AddNavigationDialog();
    }

    private void ShowQuickSettingsDialog()
    {
        if (TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            return;
        }
        mainWindow.UIHelper.GetMainView.MainPanel.Children.Add(new QuickSettingsDialog());
    }

    private void ShowQuickEditingDialog()
    {
        if (TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            return;
        }
        mainWindow.UIHelper.GetMainView.MainPanel.Children.Add(new QuickEditingDialog());
    }

    private void ShowSearchDialog()
    {
        if (TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            return;
        }
        mainWindow.AddFileSearchDialog();
    }

    public void Dispose()
    {
        Loaded -= OnLoaded;
        RemoveHandler(PointerPressedEvent, ManagePointerPressed);
        _disposables.Dispose();
    }
}
