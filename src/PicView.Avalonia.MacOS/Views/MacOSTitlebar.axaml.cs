using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Config;

namespace PicView.Avalonia.MacOS.Views;

public partial class MacOSTitlebar : UserControl
{
    public MacOSTitlebar()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (SettingsHelper.Settings.Theme.GlassTheme)
            {
                TopWindowBorder.Background = Brushes.Transparent;
            
                EditableTitlebar.Background = Brushes.Transparent;
                EditableTitlebar.BorderThickness = new Thickness(0);
                
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
        };
        PointerPressed += (_, e) => MoveWindow(e);
    }

    private void MoveWindow(PointerPressedEventArgs e)
    {
        if (VisualRoot is null) { return; }

        var hostWindow = (Window)VisualRoot;
        WindowFunctions.WindowDragAndDoubleClickBehavior(hostWindow, e);
    }
}