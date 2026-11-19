using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.SettingsManagement;

public static class ToggleHelper
{
    // Whatever stores the setting saves it (a write-through, or the window closing for a Window Setting), and what reacts to the setting subscribes (ADR-0020)
    public static ValueTask Toggle(BindableReactiveProperty<bool> setting)
    {
        setting.Value = !setting.Value;
        return ValueTask.CompletedTask;
    }
    
    public static async ValueTask ToggleScroll(MainWindowViewModel vm, MainWindow mainWindow)
    {
        var value = vm.WindowSettings.IsScrollingEnabled.CurrentValue;
        var tip = value ? TranslationManager.Translation.ScrollingDisabled : TranslationManager.Translation.ScrollingEnabled;
        _ = TooltipHelper.ShowTooltipMessageAsync(tip, false, mainWindow, TimeSpan.FromSeconds(1.5));
        await Toggle(vm.WindowSettings.IsScrollingEnabled).ConfigureAwait(false);
    }

    public static async ValueTask ToggleLooping(MainWindowViewModel vm, MainWindow mainWindow)
    {
        var value = vm.WindowSettings.IsLooping.CurrentValue;
        var tip = value ? TranslationManager.Translation.LoopingDisabled : TranslationManager.Translation.LoopingEnabled;
        _ = TooltipHelper.ShowTooltipMessageAsync(tip, false, mainWindow, TimeSpan.FromSeconds(1.5));
        await Toggle(vm.WindowSettings.IsLooping).ConfigureAwait(false);
    }

    public static async ValueTask ToggleInterface(MainWindowViewModel vm)
    {
        vm.TopTitlebarViewModel.CloseDropDownMenu();
        await Toggle(vm.WindowSettings.ShowInterface).ConfigureAwait(false);
    }

    public static ValueTask ToggleHoverBar(MainWindowViewModel vm) =>
        Toggle(vm.WindowSettings.ShowHoverBar);

    public static ValueTask ToggleSubdirectories(MainWindowViewModel vm) =>
        Toggle(vm.GlobalSettings.IsIncludingSubdirectories);

    public static ValueTask ToggleBottomToolbar(MainWindowViewModel vm) =>
        Toggle(vm.WindowSettings.ShowBottomBar);

    public static ValueTask ToggleTaskbarProgress(MainWindowViewModel vm) =>
        Toggle(vm.GlobalSettings.IsShowingTaskbarProgress);

    public static ValueTask ToggleConstrainBackgroundColor(MainWindowViewModel vm) => 
        Toggle(vm.WindowSettings.IsConstrainingBackgroundColor);
}