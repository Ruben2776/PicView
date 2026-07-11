using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Views.UC.PopUps;

public partial class RenameDialog : AnimatedPopUp
{
    public RenameDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        RenameBox.Focus();
        if (RenameBox.Text is not null)
        {
            RenameBox.CaretIndex = RenameBox.Text.Length;
        }
        
        CancelButton.Click += CloseMenu;
        CloseButton.Click += CloseMenu;
        ApplyButton.Click += ApplyButtonOnClick;
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                _ = RenameHelper.RenameAction(DataContext as MainWindowViewModel, RenameBox.Text, TopLevel.GetTopLevel(this) as MainWindow);
                e.Handled = true;
                break;
            case Key.Escape:
                CancelButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                e.Handled = true;
                break;
        }
    }

    private void ApplyButtonOnClick(object? sender, RoutedEventArgs e)
    {
        _ = RenameHelper.RenameAction(DataContext as MainWindowViewModel, RenameBox.Text, TopLevel.GetTopLevel(this) as MainWindow);
        _ = AnimatedClosing();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Loaded -= OnLoaded;
        ApplyButton.Click -= ApplyButtonOnClick;
        KeyDown -= OnKeyDown;
        CloseButton.Click -= CloseMenu;
        CancelButton.Click -= CloseMenu;
    }

    private void CloseMenu(object? sender, RoutedEventArgs e)
    {
        _ = AnimatedClosing();
    }
}
