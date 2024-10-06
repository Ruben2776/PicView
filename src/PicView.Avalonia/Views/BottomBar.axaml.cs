using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using PicView.Avalonia.UI;
using PicView.Core.Config;

namespace PicView.Avalonia.Views;

public partial class BottomBar : UserControl
{
    public BottomBar()
    {
        InitializeComponent();

        Loaded += delegate
        {
            PointerPressed += (_, e) => MoveWindow(e);
            PointerExited += (_, _) =>
            {
                DragAndDropHelper.RemoveDragDropView();
            };

            if (!SettingsHelper.Settings.Theme.GlassTheme)
            {
                return;
            }

            MainBottomBorder.Background = Brushes.Transparent;
            MainBottomBorder.BorderThickness = new Thickness(0);
                
            FileMenuButton.Background = Brushes.Transparent;
            FileMenuButton.BorderThickness = new Thickness(0);
                
            ImageMenuButton.Background = Brushes.Transparent;
            ImageMenuButton.BorderThickness = new Thickness(0);
                
            ToolsMenuButton.Background = Brushes.Transparent;
            ToolsMenuButton.BorderThickness = new Thickness(0);
                
            SettingsMenuButton.Background = Brushes.Transparent;
            SettingsMenuButton.BorderThickness = new Thickness(0);
            
            NextButton.Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255));
            NextButton.BorderThickness = new Thickness(0);
                
            PreviousButton.Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255));
            PreviousButton.BorderThickness = new Thickness(0);

            if (!Application.Current.TryGetResource("SecondaryTextColor",
                    Application.Current.RequestedThemeVariant, out var textColor))
            {
                return;
            }

            if (textColor is not Color color)
            {
                return;
            }

            FileMenuButton.Foreground = new SolidColorBrush(color);
            ImageMenuButton.Foreground = new SolidColorBrush(color);
            ToolsMenuButton.Foreground = new SolidColorBrush(color);
            SettingsMenuButton.Foreground = new SolidColorBrush(color);
            
            NextButton.Foreground = new SolidColorBrush(color);
            PreviousButton.Foreground = new SolidColorBrush(color);

        };
    }

    private void MoveWindow(PointerPressedEventArgs e)
    {
        if (VisualRoot is null) { return; }

        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            // Context menu doesn't want to be opened normally
            MainContextMenu.Open();
            return;
        }

        WindowHelper.WindowDragBehavior((Window)VisualRoot, e);
    }
}