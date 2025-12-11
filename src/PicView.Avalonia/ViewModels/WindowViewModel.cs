using PicView.Avalonia.UI;
using PicView.Core.Config;
using PicView.Core.ProcessHandling;

namespace PicView.Avalonia.ViewModels;

public class WindowViewModel
{
    public SettingsWindowConfig? SettingsWindowConfig { get; set; }
    public ImageInfoWindowConfig? ImageInfoWindowConfig { get; set; }
    public KeybindingWindowConfig? KeybindingWindowConfig { get; set; }
    public BatchResizeWindowConfig? BatchResizeWindowConfig { get; set; }
    
    public void NewWindow() => ProcessHelper.StartNewProcess();
    
    public async Task ShowImageInfoWindow()
    {
        if (UIHelper.GetMainView.DataContext is MainViewModel vm)
        {
            await vm.PlatformWindowService.ShowImageInfoWindow();
        }
    }

    public void ShowSettingsWindow()
    {
        if (UIHelper.GetMainView.DataContext is MainViewModel vm)
        {
            vm.PlatformWindowService.ShowSettingsWindow();
        }
    }

    public void ShowKeybindingsWindow()
    {
        if (UIHelper.GetMainView.DataContext is MainViewModel vm)
        {
            vm.PlatformWindowService.ShowKeybindingsWindow();
        }
    }

    public void ShowAboutWindow()
    {
        if (UIHelper.GetMainView.DataContext is MainViewModel vm)
        {
            vm.PlatformWindowService.ShowAboutWindow();
        }
    }

    public void ShowConvertWindow()
    {
        if (UIHelper.GetMainView.DataContext is MainViewModel vm)
        {
            vm.PlatformWindowService.ShowConvertWindow();
        }
    }

    public void ShowBatchResizeWindow()
    {
        if (UIHelper.GetMainView.DataContext is MainViewModel vm)
        {
            vm.PlatformWindowService.ShowBatchResizeWindow();
        }
    }

    public void ShowSingleImageResizeWindow()
    {
        if (UIHelper.GetMainView.DataContext is MainViewModel vm)
        {
            vm.PlatformWindowService.ShowSingleImageResizeWindow();
        }
    }

    public void ShowEffectsWindow()
    {
        if (UIHelper.GetMainView.DataContext is MainViewModel vm)
        {
            vm.PlatformWindowService.ShowEffectsWindow();
        }
    }
}