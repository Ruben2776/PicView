using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Views.UC.PopUps;

public partial class FavoriteDialog : AnimatedPopUp
{
    public FavoriteDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        
        CancelButton.Click += CloseMenu;
        CloseButton.Click += CloseMenu;
        SaveButton.Click += ApplyButtonOnClick;
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                //
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
        //
        _ = AnimatedClosing();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Loaded -= OnLoaded;
        SaveButton.Click -= ApplyButtonOnClick;
        KeyDown -= OnKeyDown;
        CloseButton.Click -= CloseMenu;
        CancelButton.Click -= CloseMenu;
    }

    private void CloseMenu(object? sender, RoutedEventArgs e)
    {
        _ = AnimatedClosing();
    }
}
