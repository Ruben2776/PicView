using PicView.WPF.UILogic.Loading;

namespace PicView.WPF.Views.Windows;

public partial class Startup_Window
{
    public Startup_Window()
    {
        InitializeComponent();

        ContentRendered += async (_, _) => await StartLoading.LoadedEvent(this).ConfigureAwait(false);
    }
}