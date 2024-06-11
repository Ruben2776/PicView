using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using PicView.Avalonia.Helpers;

namespace PicView.Avalonia.Views;

public partial class BottomBar : UserControl
{
    public BottomBar()
    {
        InitializeComponent();
        NextButton.PointerEntered += (s, e) =>
        {
            if (!Application.Current.TryGetResource("ButtonForegroundPointerOver", ThemeVariant.Default, out var buttonForegroundPointerOver))
            {
                return;
            }
            var brush = new SolidColorBrush((Color)(buttonForegroundPointerOver ?? Brushes.White));
            NextIcon.Fill = brush;
        };
        NextButton.PointerExited += (s, e) =>
        {
            if (!Application.Current.TryGetResource("MainIconColor", ThemeVariant.Default, out var mainIconColor))
            {
                return;
            }
            var brush = new SolidColorBrush((Color)(mainIconColor ?? Brushes.White));
            NextIcon.Fill = brush;
        };
        PreviousButton.PointerEntered += (s, e) =>
        {
            if (!Application.Current.TryGetResource("ButtonForegroundPointerOver", ThemeVariant.Default, out var buttonForegroundPointerOver))
            {
                return;
            }
            var brush = new SolidColorBrush((Color)(buttonForegroundPointerOver ?? Brushes.White));
            PrevIcon.Fill = brush;
        };
        PreviousButton.PointerExited += (s, e) =>
        {
            if (!Application.Current.TryGetResource("MainIconColor", ThemeVariant.Default, out var mainIconColor))
            {
                return;
            }
            var brush = new SolidColorBrush((Color)(mainIconColor ?? Brushes.White));
            PrevIcon.Fill = brush;
        };
        PointerPressed += (_, e) => MoveWindow(e);
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

        var hostWindow = (Window)VisualRoot;
        hostWindow.BeginMoveDrag(e);
    }
}