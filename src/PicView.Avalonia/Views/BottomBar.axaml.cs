using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using PicView.Avalonia.UI;

namespace PicView.Avalonia.Views;

public partial class BottomBar : UserControl
{
    public BottomBar()
    {
        InitializeComponent();
        NextButton.PointerEntered += (s, e) =>
        {
            if (!Application.Current.TryGetResource("ButtonForegroundPointerOver", Application.Current.RequestedThemeVariant, out var buttonForegroundPointerOver))
            {
                return;
            }
            var brush = new SolidColorBrush((Color)(buttonForegroundPointerOver ?? Brushes.White));
            NextIcon.Fill = brush;
        };
        NextButton.PointerExited += (s, e) =>
        {
            if (!Application.Current.TryGetResource("MainTextColor", Application.Current.RequestedThemeVariant, out var MainTextColor))
            {
                return;
            }
            var brush = new SolidColorBrush((Color)(MainTextColor ?? Brushes.White));
            NextIcon.Fill = brush;
        };
        PreviousButton.PointerEntered += (s, e) =>
        {
            if (!Application.Current.TryGetResource("ButtonForegroundPointerOver", Application.Current.RequestedThemeVariant, out var buttonForegroundPointerOver))
            {
                return;
            }
            var brush = new SolidColorBrush((Color)(buttonForegroundPointerOver ?? Brushes.White));
            PrevIcon.Fill = brush;
        };
        PreviousButton.PointerExited += (s, e) =>
        {
            if (!Application.Current.TryGetResource("MainTextColor", Application.Current.RequestedThemeVariant, out var MainTextColor))
            {
                return;
            }
            var brush = new SolidColorBrush((Color)(MainTextColor ?? Brushes.White));
            PrevIcon.Fill = brush;
        };
        PointerPressed += (_, e) => MoveWindow(e);
        PointerExited += (_, _) =>
        {
            DragAndDropHelper.RemoveDragDropView();
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