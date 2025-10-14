using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using PicView.Avalonia.Functions;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC.PopUps;
using PicView.Core.Sizing;
using R3;

namespace PicView.Avalonia.Views.UC;

public partial class HoverBar : UserControl
{
    public HoverBar()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        AddHandler(PointerPressedEvent, ManagePointerPressed, RoutingStrategies.Tunnel);
        SizeChanged += (_, args) => ApplyResponsiveResize(args.NewSize.Width);
        ApplyResponsiveResize(Bounds.Width);

        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        Observable.FromEventHandler<RoutedEventArgs>(h => NextButton.Click += h,
                h => NextButton.Click -= h)
            .SubscribeAwait(async (_, c) =>
            {
                vm.HoverbarViewModel.IsHoverNavigationButtonNextClicked = true;
                await NavigationManager.Navigate(true, vm, c);
            });
        Observable.FromEventHandler<RoutedEventArgs>(h => PreviousButton.Click += h,
                h => PreviousButton.Click -= h)
            .SubscribeAwait(async (_, c) =>
            {
                vm.HoverbarViewModel.IsHoverNavigationButtonPreviousClicked = true;
                await NavigationManager.Navigate(false, vm, c);
            });
    }

    private void ApplyResponsiveResize(double width)
    {
        const int firstBreakpoint = 475;
        const int secondBreakpoint = 550;
        const int thirdBreakpoint = 800;

        switch (width)
        {
            case < SizeDefaults.WindowMinSize:
                // Too small to fit
                IsVisible = false;
                break;
            case <= firstBreakpoint:
                ApplyLayout(
                    70,
                    false,
                    true,
                    false,
                    new Thickness(5, 45, 5, 0));
                break;

            case <= secondBreakpoint:
                ApplyLayout(
                    75,
                    false,
                    true,
                    false,
                    new Thickness(5, 45, 5, 0));
                break;

            case < thirdBreakpoint:
                ApplyLayout(
                    72,
                    false,
                    false,
                    true,
                    new Thickness(5, 65, 5, 0));
                break;

            default:
                ApplyLayout(
                    75,
                    true,
                    false,
                    true,
                    new Thickness(5, 65, 5, 0));
                break;
        }
    }

    private void ApplyLayout(double buttonWidth, bool showRotateLeft, bool showTopBorder, bool showAdvancedButtons,
        Thickness topPanelMargin)
    {
        NextButton.Width = PreviousButton.Width = buttonWidth;
        RotateLeftButton.IsVisible = showRotateLeft;
        TopBorder.IsVisible = showTopBorder;
        RotateRightButton.IsVisible =
            FlipButton.IsVisible =
                ZoomInMenuButton.IsVisible =
                    ZoomOutMenuButton.IsVisible = showAdvancedButtons;
        TopPanel.Margin = topPanelMargin;

        IsVisible = true;
    }


    private async Task ManagePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
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
                await FunctionsMapper.SettingsWindow();
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
                vm.HoverbarViewModel.IsHoverRotateLeftClicked = true;
            }
        }
        else if (RotateRightButton.IsPointerOver)
        {
            if (props.IsLeftButtonPressed)
            {
                vm.HoverbarViewModel.IsHoverRotateRightClicked = true;
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
                ShowMainContextMenu();
            }
        }
    }

    private static void ShowNavigationDialog() =>
        UIHelper.GetMainView.MainGrid.Children.Add(new NavigationDialog());

    private static void ShowQuickSettingsDialog() =>
        UIHelper.GetMainView.MainGrid.Children.Add(new QuickSettingsDialog());
    
    private static void ShowQuickEditingDialog() =>
        UIHelper.GetMainView.MainGrid.Children.Add(new QuickEditingDialog());

    private static void ShowSearchDialog() =>
        UIHelper.GetMainView.MainGrid.Children.Add(new FileSearchDialog());

    private static void ShowMainContextMenu()
    {
        if (UIHelper.GetMainView.Resources.TryGetResource("MainContextMenu", Application.Current.ActualThemeVariant, out var value)
            && value is ContextMenu mainContextMenu)
        {
            mainContextMenu.Open();
        }
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        Loaded -= OnLoaded;
        RemoveHandler(PointerPressedEvent, ManagePointerPressed);
    }
}
