using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Media;
using PicView.Avalonia.CustomControls;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Win32.Views;

public partial class SingleImageResizeWindow : GenericWindow
{
    public SingleImageResizeWindow(MainWindowViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}