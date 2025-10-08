using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.DragAndDrop;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.WindowBehavior;

namespace PicView.Avalonia.Views.UC;

public partial class BottomBar : UserControl
{
    public BottomBar()
    {
        InitializeComponent();

        Loaded += delegate
        {
            PointerPressed += (_, e) => MoveWindow(e);
            PointerExited += (_, _) => { DragAndDropHelper.RemoveDragDropView(); };

            if (DataContext is not MainViewModel vm)
            {
                return;
            }

            PreviousButton.Click += (_, _) =>
            {
                vm.MainWindow.IsNavigationButtonLeftClicked = true;
                UIHelper.SetButtonInterval((IconButton?)PreviousButton);
            };
            NextButton.Click += (_, _) =>
            {
                vm.MainWindow.IsNavigationButtonRightClicked = true;
                UIHelper.SetButtonInterval((IconButton?)NextButton);
            };

            RotateRightButton.Click += (_, _) => { vm.MainWindow.IsBottomToolbarRotationClicked = true; };

            if (!Application.Current.TryGetResource("SecondaryTextColor",
                    Application.Current.RequestedThemeVariant, out var textColor))
            {
                return;
            }

            if (textColor is not Color color)
            {
                return;
            }

            if (Settings.Theme.GlassTheme)
            {
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

                FileMenuButton.Foreground = new SolidColorBrush(color);
                ImageMenuButton.Foreground = new SolidColorBrush(color);
                ToolsMenuButton.Foreground = new SolidColorBrush(color);
                SettingsMenuButton.Foreground = new SolidColorBrush(color);

                NextButton.Foreground = new SolidColorBrush(color);
                PreviousButton.Foreground = new SolidColorBrush(color);
            }
            else if (!Settings.Theme.Dark)
            {
                FileMenuButton.Classes.Remove("noBorderHover");
                FileMenuButton.Classes.Add("noBorderHoverAlt");

                ImageMenuButton.Classes.Remove("noBorderHover");
                ImageMenuButton.Classes.Add("noBorderHoverAlt");
                if (TryGetResource("ImageMenuBrush", Application.Current.RequestedThemeVariant,
                        out var imageMenuBrush))
                {
                    if (imageMenuBrush is SolidColorBrush brush)
                    {
                        UIHelper.SetButtonHover(ImageMenuButton, brush);
                    }
                }

                ToolsMenuButton.Classes.Remove("noBorderHover");
                ToolsMenuButton.Classes.Add("noBorderHoverAlt");
                if (TryGetResource("ToolsMenuBrush", Application.Current.RequestedThemeVariant,
                        out var toolsMenuBrush))
                {
                    if (toolsMenuBrush is SolidColorBrush brush)
                    {
                        UIHelper.SetButtonHover(ToolsMenuButton, brush);
                    }
                }

                SettingsMenuButton.Classes.Remove("noBorderHover");
                SettingsMenuButton.Classes.Add("noBorderHoverAlt");

                ZoomOutButton.Classes.Remove("noBorderHover");
                ZoomInButton.Classes.Remove("noBorderHover");
                ZoomOutButton.Classes.Add("noBorderHoverAlt");
                ZoomInButton.Classes.Add("noBorderHoverAlt");

                RotateRightButton.Classes.Remove("noBorderHover");
                FlipButton.Classes.Remove("noBorderHover");
                RotateRightButton.Classes.Add("noBorderHoverAlt");
                FlipButton.Classes.Add("noBorderHoverAlt");
            }
        };
    }

    private void MoveWindow(PointerPressedEventArgs e)
    {
        if (VisualRoot is null)
        {
            return;
        }

        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            // Context menu doesn't want to be opened normally
            MainContextMenu.Open();
            return;
        }

        WindowFunctions.WindowDragBehavior((Window)VisualRoot, e);
    }
}