using Avalonia;
using Avalonia.Controls;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

namespace PicView.Tests.WindowBehavior;

[Collection("Sequential")]
public class WindowFitterTests
{
    private static readonly Size ContentSize = new(800, 600);

    private readonly CoreViewModel _core;

    public WindowFitterTests()
    {
        SetDefaults();
        TranslationManager.Init();
        Settings.WindowProperties.AutoFit = false;
        Settings.ImageScaling.ZoomToFit = false;
        _core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()));
    }

    [AvaloniaFact]
    public void Refit_ImageSmallerThanTheWindow_IsShownAtItsPixelSize()
    {
        var (fitter, vm, window) = CreateWindow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300));

        fitter.Refit();

        Assert.Equal(400, vm.ImageWidth.Value);
        Assert.Equal(300, vm.ImageHeight.Value);
        Assert.Equal(SizeToContent.Manual, window.SizeToContent);
    }

    [AvaloniaFact]
    public void Refit_ImageLargerThanTheWindow_FitsInsideIt()
    {
        var (fitter, vm, _) = CreateWindow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 1600, 600));

        fitter.Refit();

        Assert.Equal(800, vm.ImageWidth.Value);
        Assert.Equal(300, vm.ImageHeight.Value);
    }

    [AvaloniaFact]
    public void Refit_PairWhoseSecondSizeIsNotKnownYet_WaitsInsteadOfFittingOneImage()
    {
        Settings.ImageScaling.ShowImageSideBySide = true;
        var (fitter, vm, _) = CreateWindow();
        vm.WindowTabs.ActiveTab.Value.Display(Model("a.jpg", 400, 300), Model("b.jpg", 0, 0));

        fitter.Refit();

        Assert.True(double.IsNaN(vm.ImageWidth.Value));
    }

    [AvaloniaFact]
    public void FitBeforeShowing_KnownSizes_FitsAndSaysSo()
    {
        var (fitter, vm, _) = CreateWindow();

        Assert.True(fitter.FitBeforeShowing(new PixelSizes(400, 300)));
        Assert.Equal(400, vm.ImageWidth.Value);
    }

    [AvaloniaFact]
    public void FitBeforeShowing_NoSize_LeavesTheWindowAlone()
    {
        var (fitter, vm, _) = CreateWindow();

        Assert.False(fitter.FitBeforeShowing(new PixelSizes(0, 0)));
        Assert.True(double.IsNaN(vm.ImageWidth.Value));
    }

    [AvaloniaTheory]
    [InlineData(true, SizeToContent.WidthAndHeight)]
    [InlineData(false, SizeToContent.Manual)]
    public void FollowContent_OnlyWithAutoFit(bool autoFit, SizeToContent expected)
    {
        Settings.WindowProperties.AutoFit = autoFit;
        var (fitter, _, window) = CreateWindow();

        fitter.FollowContent();

        Assert.Equal(expected, window.SizeToContent);
    }

    [AvaloniaFact]
    public void KeepSize_StopsFollowingContentAndLiftsTheLimit()
    {
        var (fitter, vm, window) = CreateWindow();
        window.SizeToContent = SizeToContent.WidthAndHeight;
        vm.WindowMaxWidth.Value = 500;

        fitter.KeepSize();

        Assert.Equal(SizeToContent.Manual, window.SizeToContent);
        Assert.True(double.IsNaN(vm.WindowMaxWidth.Value));
    }

    private (WindowFitter Fitter, MainWindowViewModel Vm, MainWindow Window) CreateWindow()
    {
        var window = new TestMainWindow(new NativeStateAdapter());
        var vm = new MainWindowViewModel(_core, _core.Translation, window, _core.GlobalSettings!,
            _core.GallerySettings);
        window.DataContext = vm;
        return (new WindowFitter(window, () => ContentSize), vm, window);
    }

    private static ImageModel Model(string name, uint width, uint height) =>
        new() { FileInfo = new FileInfo(name), PixelWidth = width, PixelHeight = height };

    private sealed class NativeStateAdapter : IWindowModeAdapter
    {
        public bool SkipsInitialNativeState => false;

        public void EnterNativeState(MainWindow window, WindowModeState state) =>
            WindowModeSwitcher.SetNativeState(window, state.Mode);
    }
}
