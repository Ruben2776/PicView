using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;

namespace PicView.Avalonia.Win32.Views;

public partial class WinTitleBar : UserControl
{
    public WinTitleBar()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (SettingsHelper.Settings.Theme.GlassTheme)
            {
                TopWindowBorder.Background = Brushes.Transparent;
                TopWindowBorder.BorderThickness = new Thickness(0);
            
                LogoBorder.Background = Brushes.Transparent;
                LogoBorder.BorderThickness = new Thickness(0);
            
                LogoBorder.Background = Brushes.Transparent;
                LogoBorder.BorderThickness = new Thickness(0);
            
                EditableTitlebar.Background = Brushes.Transparent;
                EditableTitlebar.BorderThickness = new Thickness(0);
                
                CloseButton.Background = Brushes.Transparent;
                CloseButton.BorderThickness = new Thickness(0);
                
                MinimizeButton.Background = Brushes.Transparent;
                MinimizeButton.BorderThickness = new Thickness(0);
                
                RestoreButton.Background = Brushes.Transparent;
                RestoreButton.BorderThickness = new Thickness(0);
                
                FlipButton.Background = Brushes.Transparent;
                FlipButton.BorderThickness = new Thickness(0);
                
                GalleryButton.Background = Brushes.Transparent;
                GalleryButton.BorderThickness = new Thickness(0);
                
                RotateRightButton.Background = Brushes.Transparent;
                RotateRightButton.BorderThickness = new Thickness(0);
                
                if (!Application.Current.TryGetResource("SecondaryTextColor", Application.Current.RequestedThemeVariant, out var color))
                {
                    return;
                }

                if (color is not Color secondaryTextColor)
                {
                    return;
                }

                try
                {
                    EditableTitlebar.Foreground = new SolidColorBrush(secondaryTextColor);
                    CloseButton.Foreground = new SolidColorBrush(secondaryTextColor);
                    MinimizeButton.Foreground = new SolidColorBrush(secondaryTextColor);
                    RestoreButton.Foreground = new SolidColorBrush(secondaryTextColor);
                    FlipButton.Foreground = new SolidColorBrush(secondaryTextColor);
                    GalleryButton.Foreground = new SolidColorBrush(secondaryTextColor);
                    RotateRightButton.Foreground = new SolidColorBrush(secondaryTextColor);
                }
                #if DEBUG
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                #else
                catch (Exception) { }
                #endif
            }
            PointerPressed += (_, e) => MoveWindow(e);
            PointerExited += (_, _) =>
            {
                DragAndDropHelper.RemoveDragDropView();
            };
        };

    }

    private void MoveWindow(PointerPressedEventArgs e)
    {
        if (VisualRoot is null) { return; }

        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        if (vm.IsEditableTitlebarOpen)
        {
            return;
        }
        WindowHelper.WindowDragAndDoubleClickBehavior((Window)VisualRoot, e);
    }
}