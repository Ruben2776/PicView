using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Input;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using PicView.Tests.Navigation;
using PicView.Tests.WindowBehavior;

namespace PicView.Tests.Input;

/// <summary>
/// A slideshow puts only its own Main Window in the Slideshow Input Mode, and other windows' Keybinds leave it alone.
/// </summary>
[Collection("Sequential")]
public class SlideshowInputModeTests
{
    private readonly ManualTimeProvider _clock = new();

    public SlideshowInputModeTests()
    {
        SetDefaults();
        TranslationManager.Init();
    }

    [AvaloniaFact]
    public void StateOf_OtherWindowRunsSlideshow_IsNotSlideshow()
    {
        var (a, aVm) = CreateWindow();
        var (b, _) = CreateWindow();
        try
        {
            aVm.Slideshow.Toggle(60000, isKeyHeldDown: false);

            Assert.Equal(InputMode.Slideshow, InputModes.StateOf(a).Resolve());
            Assert.NotEqual(InputMode.Slideshow, InputModes.StateOf(b).Resolve());
        }
        finally
        {
            aVm.Slideshow.Dispose();
            a.Close();
            b.Close();
        }
    }

    [AvaloniaFact]
    public async Task Cancel_InOtherWindow_LeavesSlideshowRunning()
    {
        var (a, aVm) = CreateWindow();
        var (b, bVm) = CreateWindow();
        try
        {
            aVm.Slideshow.Toggle(60000, isKeyHeldDown: false);

            await new ContextualActionHandler(b, bVm).TryRunAsync(InputMode.Slideshow, ContextualActionHandler.Cancel);

            Assert.True(aVm.Slideshow.IsRunning);
        }
        finally
        {
            aVm.Slideshow.Dispose();
            a.Close();
            b.Close();
        }
    }

    private (TestMainWindow Window, MainWindowViewModel Vm) CreateWindow()
    {
        var core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()), timeProvider: _clock);

        // Two files and at the first, so the slideshow has somewhere to go
        var tab = new TabViewModel(null!,
            TestTabServices.For(new SharedImageCache(f => ValueTask.FromResult(new ImageModel { FileInfo = f })), null));
        tab.Initialize();
        tab.ImageIterator!.Initialize([new FileInfo("a.jpg"), new FileInfo("b.jpg")]);

        var adapter = new NativeStateAdapter();
        var window = new TestMainWindow(adapter);
        var vm = new MainWindowViewModel(core, core.Translation, core.GlobalSettings!, core.GallerySettings, tab);
        window.DataContext = vm;
        window.ViewModel = vm;
        var switcher = new WindowModeSwitcher(window, adapter);
        vm.WindowModeSwitcher = switcher;
        switcher.Start(isAppStart: true);
        window.Show();
        return (window, vm);
    }

    private sealed class NativeStateAdapter : IWindowModeAdapter
    {
        public bool SkipsInitialNativeState => false;

        public void EnterNativeState(MainWindow window, WindowModeState state) =>
            WindowModeSwitcher.SetNativeState(window, state.Mode);
    }
}
