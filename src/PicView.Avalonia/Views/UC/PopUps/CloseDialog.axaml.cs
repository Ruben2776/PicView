using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using PicView.Avalonia.CustomControls;

namespace PicView.Avalonia.Views.UC.PopUps;

public partial class CloseDialog : AnimatedPopUp
{
    public CloseDialog()
    {
        InitializeComponent();
        CancelButton.Click += async delegate
        {
            await AnimatedClosing();
        };
        CloseButton.Click += delegate
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }
            desktop.MainWindow?.Close();
        };
        
        Focus();
        
        KeyDown += (_, e) =>
        {
            switch (e.Key)
            {
                case Key.Enter:
                    CloseButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    break;
                case Key.Escape:
                    CancelButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    break;
            }
            e.Handled = true;
        };
    }
}
