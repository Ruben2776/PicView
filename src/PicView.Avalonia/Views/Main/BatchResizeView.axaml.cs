using Avalonia.Controls;

namespace PicView.Avalonia.Views.Main;

public partial class BatchResizeView : UserControl
{
    public BatchResizeView()
    {
        InitializeComponent();
        // Shown only while no run is going, so it just closes the window. It has no command, since closing disposes
        // the commands before a bound one would run.
        CancelButton.Click += delegate
        {
            if (TopLevel.GetTopLevel(this) is Window window)
            {
                window.Close();
            }
        };
    }
}
