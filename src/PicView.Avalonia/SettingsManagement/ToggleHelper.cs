using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.SettingsManagement;

public static class ToggleHelper
{
    // The write-through saves, and what reacts to the setting subscribes (ADR-0020)
    public static ValueTask Toggle(BindableReactiveProperty<bool> setting)
    {
        setting.Value = !setting.Value;
        return ValueTask.CompletedTask;
    }
    
    public static async ValueTask ToggleScroll(MainWindowViewModel vm, MainWindow mainWindow)
    {
        var value = vm.GlobalSettings.IsScrollingEnabled.CurrentValue;
        var tip = value ? TranslationManager.Translation.ScrollingDisabled : TranslationManager.Translation.ScrollingEnabled;
        _ = TooltipHelper.ShowTooltipMessageAsync(tip, false, mainWindow, TimeSpan.FromSeconds(1.5));
        await Toggle(vm.GlobalSettings.IsScrollingEnabled).ConfigureAwait(false);
    }

    public static async ValueTask ToggleLooping(MainWindowViewModel vm, MainWindow mainWindow)
    {
        var value = vm.GlobalSettings.IsLooping.CurrentValue;
        var tip = value ? TranslationManager.Translation.LoopingDisabled : TranslationManager.Translation.LoopingEnabled;
        _ = TooltipHelper.ShowTooltipMessageAsync(tip, false, mainWindow, TimeSpan.FromSeconds(1.5));
        await Toggle(vm.GlobalSettings.IsLooping).ConfigureAwait(false);
    }

    public static async ValueTask ToggleInterface(MainWindowViewModel vm)
    {
        vm.TopTitlebarViewModel.CloseDropDownMenu();
        await Toggle(vm.GlobalSettings.ShowInterface).ConfigureAwait(false);
    }

    public static ValueTask ToggleHoverBar(MainWindowViewModel vm) => Toggle(vm.GlobalSettings.ShowHoverBar);

    public static ValueTask ToggleSubdirectories(MainWindowViewModel vm) => Toggle(vm.GlobalSettings.IsIncludingSubdirectories);

    public static ValueTask ToggleBottomToolbar(MainWindowViewModel vm) => Toggle(vm.GlobalSettings.ShowBottomBar);

    public static ValueTask ToggleTaskbarProgress(MainWindowViewModel vm) => Toggle(vm.GlobalSettings.IsShowingTaskbarProgress);

    public static ValueTask ToggleConstrainBackgroundColor(MainWindowViewModel vm) => Toggle(vm.GlobalSettings.IsConstrainingBackgroundColor);
}