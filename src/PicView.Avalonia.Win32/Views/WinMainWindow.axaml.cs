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

    /// <inheritdoc />
    public override async Task Maximize(bool saveSetting = true) =>
        await Win32Window.Maximize(this, (DataContext as MainWindowViewModel)!, saveSetting);
    
    /// <inheritdoc />
    public override async Task MaximizeRestore(bool saveSetting = true) =>
        await Win32Window.ToggleMaximize(this, (DataContext as MainWindowViewModel)!, saveSetting);

    /// <inheritdoc />
    public override async Task Fullscreen(bool saveSetting = true) =>
        await Win32Window.Fullscreen(this, (DataContext as MainWindowViewModel)!, saveSetting);
    
    /// <inheritdoc />
    public override async Task ToggleFullscreen(bool saveSetting = true) =>
        await Win32Window.ToggleFullscreen(this, (DataContext as MainWindowViewModel)!, saveSetting);
    
    /// <inheritdoc />
    public override async Task Restore() =>
        await Win32Window.Restore(this, (DataContext as MainWindowViewModel)!);
    
    public override void Minimize() =>
        Win32Window.Minimize(this);
}
