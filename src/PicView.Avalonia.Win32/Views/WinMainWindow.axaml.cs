using System.Diagnostics;
using Avalonia;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Win32.WindowImpl;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Win32.Views;

public partial class WinMainWindow : MainWindow
{
    public WinMainWindow()
    {
        if (Application.Current!.DataContext is not CoreViewModel core)
        {
            return;
        }

        Debug.Assert(core.GlobalSettings != null);
        var mainWindowViewModel = new MainWindowViewModel(core, core.Translation, this, core.GlobalSettings, core.GallerySettings);
        DataContext = mainWindowViewModel;

        InitializeComponent();
        
        SharedBottomBar = BottomBar;
        SharedTitleBar = Titlebar;
        SharedMainView = MainView;
        UIHelper.Initialize(this);
    }

    protected override IWindowProvider CreateWindowProvider() => new Win32WindowProvider();
}
