using Avalonia;
using Avalonia.Controls;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.MacOS.WindowImpl;
using PicView.Core.DebugTools;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.MacOS.Views;

public partial class MacMainWindow : MainWindow
{
    public MacMainWindow() : this(null)
    {
    }

    /// <param name="firstTab">A tab moved out of another Main Window, for a window that opens with it.</param>
    public MacMainWindow(CoreViewModel? core, TabViewModel? firstTab = null)
    {
        core ??= Application.Current?.DataContext as CoreViewModel;
        if (core is null)
        {
            return;
        }
        var mainWindowViewModel = new MainWindowViewModel(core, core.Translation, this, core.GlobalSettings, core.GallerySettings, firstTab);
        DataContext = mainWindowViewModel;

        InitializeComponent();
        
        SharedBottomBar = BottomBar;
        SharedTitleBar = Titlebar;
        SharedMainView = MainView;
        UIHelper.Initialize(this);

        Loaded += delegate
        {
            if (DataContext is not MainWindowViewModel vm)
            {
                return;
            }
            
            // Hide macOS traffic lights buttons when interface is hidden
            Observable.EveryValueChanged(vm, x => x.IsTopToolbarShown.CurrentValue, FrameProvider).Subscribe(shown =>
            {
                if (vm.IsFullscreen.Value)
                {
                    WindowDecorations = WindowDecorations.Full;
                }
                else
                {
                    WindowDecorations = shown ? WindowDecorations.Full : WindowDecorations.None;
                }
            }, static result =>
            {
#if DEBUG
                if (result is { IsFailure: true, Exception: not null })
                {
                    DebugHelper.LogDebug(nameof(MacMainWindow), nameof(vm.IsTopToolbarShown), result.Exception);
                }
#endif
            })
            .AddTo(Disposables);
        };
    }

    protected override IWindowProvider CreateWindowProvider() => new MacWindowProvider();

    public override int CombinedTitleButtonsWidth { get; set; } = 165;
}
