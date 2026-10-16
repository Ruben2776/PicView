using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using PicView.Avalonia.CustomControls;

namespace PicView.Avalonia.Views.UC.PopUps;

public partial class CloseDialog : AnimatedPopUp
{
    public CloseDialog()
    {
        InitializeComponent();
        CancelButton.Click += async delegate
        {
            await AnimatedClosing().ConfigureAwait(true);
        };
        CloseButton.Click += delegate { CloseWindow(); };
    }

    /// <summary>
    /// Closes the window, as the Close button does.
    /// </summary>
    public override ValueTask<bool> ConfirmAsync()
    {
        CloseWindow();
        return ValueTask.FromResult(true);
    }

    private void CloseWindow()
    {
        if (TopLevel.GetTopLevel(this) is Window window)
        {
            window.Close();
            return;
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow?.Close();
        }
    }
}
