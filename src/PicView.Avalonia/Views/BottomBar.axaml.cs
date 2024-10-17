using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using PicView.Avalonia.UI;
using PicView.Avalonia.WindowBehavior;
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
            FileMenuButton.Classes.Remove("noBorderHover");
            FileMenuButton.Classes.Add("hover");
                
            ImageMenuButton.Background = Brushes.Transparent;
            ImageMenuButton.Classes.Remove("noBorderHover");
            ImageMenuButton.Classes.Add("hover");
                
            ToolsMenuButton.Background = Brushes.Transparent;
            ToolsMenuButton.Classes.Remove("noBorderHover");
            ToolsMenuButton.Classes.Add("hover");
                
            SettingsMenuButton.Background = Brushes.Transparent;
            SettingsMenuButton.Classes.Remove("noBorderHover");
            SettingsMenuButton.Classes.Add("hover");
            
            NextButton.Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255));
                
            PreviousButton.Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255));

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

        WindowFunctions.WindowDragBehavior((Window)VisualRoot, e);
    }
}