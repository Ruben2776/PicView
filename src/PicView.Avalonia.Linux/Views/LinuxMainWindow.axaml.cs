using System.Diagnostics;
using Avalonia;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Linux.WindowImpl;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Linux.Views;

public partial class LinuxMainWindow : MainWindow
{
    public LinuxMainWindow()
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

    protected override IWindowProvider CreateWindowProvider() => new LinuxWindowProvider();

    /// <inheritdoc />
    public override async Task Maximize(bool saveSetting = true) =>
        await LinuxWindow.Maximize(this, (DataContext as MainWindowViewModel)!, saveSetting);
    
    /// <inheritdoc />
    public override async Task MaximizeRestore(bool saveSetting = true) =>
        await LinuxWindow.ToggleMaximize(this, (DataContext as MainWindowViewModel)!, saveSetting);

    /// <inheritdoc />
    public override async Task Fullscreen(bool saveSetting = true) =>
        await LinuxWindow.Fullscreen(this, (DataContext as MainWindowViewModel)!, saveSetting);
    
    /// <inheritdoc />
    public override async Task ToggleFullscreen(bool saveSetting = true) =>
        await LinuxWindow.ToggleFullscreen(this, (DataContext as MainWindowViewModel)!, saveSetting);
    
    /// <inheritdoc />
    public override async Task Restore() =>
        await LinuxWindow.Restore(this, (DataContext as MainWindowViewModel)!);
    
    public override void Minimize() =>
        LinuxWindow.Minimize(this);
}
