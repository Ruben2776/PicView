using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.SettingsManagement;

public static class ToggleHelper
{
    /// <summary>
    /// Toggles an App Setting. Whatever stores the setting saves it, and what reacts to it subscribes (ADR-0020).
    /// </summary>
    public static ValueTask Toggle(BindableReactiveProperty<bool> setting)
    {
        setting.Value = !setting.Value;
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Toggles a Window Setting of the window <paramref name="settings"/> belong to, through <see cref="ChangeWindowSetting"/>.
    /// </summary>
    public static ValueTask Toggle(WindowSettings settings, Func<WindowSettings, BindableReactiveProperty<bool>> pick)
    {
        ChangeWindowSetting(settings, pick, static on => !on);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// The one way an Action or the Settings window changes a Window Setting: sets the setting
    /// <paramref name="pick"/> chooses to <paramref name="next"/> of its value. A closed window's settings take no
    /// change, as setting a disposed bindable throws (ADR-0038), so this does nothing once the window closed, and
    /// nothing either when the close lands between the check and the set. Nothing is written to the settings record;
    /// the window stores its settings when it closes.
    /// </summary>
    public static void ChangeWindowSetting<T>(WindowSettings settings, Func<WindowSettings, BindableReactiveProperty<T>> pick,
        Func<T, T> next)
    {
        if (settings.IsDisposed)
        {
            return;
        }

        var setting = pick(settings);
        try
        {
            setting.Value = next(setting.Value);
        }
        catch (ObjectDisposedException)
        {
            // The window closed between the check and the set
        }
    }

    public static async ValueTask ToggleScroll(MainWindowViewModel vm, MainWindow mainWindow)
    {
        var value = vm.WindowSettings.IsScrollingEnabled.CurrentValue;
        var tip = value ? TranslationManager.Translation.ScrollingDisabled : TranslationManager.Translation.ScrollingEnabled;
        _ = TooltipHelper.ShowTooltipMessageAsync(tip, false, mainWindow, TimeSpan.FromSeconds(1.5));
        await Toggle(vm.WindowSettings, static s => s.IsScrollingEnabled).ConfigureAwait(false);
    }

    public static async ValueTask ToggleLooping(MainWindowViewModel vm, MainWindow mainWindow)
    {
        var value = vm.WindowSettings.IsLooping.CurrentValue;
        var tip = value ? TranslationManager.Translation.LoopingDisabled : TranslationManager.Translation.LoopingEnabled;
        _ = TooltipHelper.ShowTooltipMessageAsync(tip, false, mainWindow, TimeSpan.FromSeconds(1.5));
        await Toggle(vm.WindowSettings, static s => s.IsLooping).ConfigureAwait(false);
    }

    public static async ValueTask ToggleInterface(MainWindowViewModel vm)
    {
        vm.TopTitlebarViewModel.CloseDropDownMenu();
        await Toggle(vm.WindowSettings, static s => s.ShowInterface).ConfigureAwait(false);
    }

    public static ValueTask ToggleHoverBar(MainWindowViewModel vm) =>
        Toggle(vm.WindowSettings, static s => s.ShowHoverBar);

    public static ValueTask ToggleSubdirectories(MainWindowViewModel vm) =>
        Toggle(vm.GlobalSettings.IsIncludingSubdirectories);

    /// <summary>
    /// Does nothing where there is no file manager to follow, as on Linux: turned on there, it would disable the
    /// sort options with no menu item to turn it off again.
    /// </summary>
    public static ValueTask ToggleFollowSystemSorting(MainWindowViewModel vm) =>
        vm.GlobalSettings.CanFollowSystemSorting
            ? Toggle(vm.GlobalSettings.IsFollowingSystemSorting)
            : ValueTask.CompletedTask;

    public static ValueTask ToggleBottomToolbar(MainWindowViewModel vm) =>
        Toggle(vm.WindowSettings, static s => s.ShowBottomBar);

    public static ValueTask ToggleTaskbarProgress(MainWindowViewModel vm) =>
        Toggle(vm.GlobalSettings.IsShowingTaskbarProgress);

    public static ValueTask ToggleConstrainBackgroundColor(MainWindowViewModel vm) => 
        Toggle(vm.WindowSettings, static s => s.IsConstrainingBackgroundColor);
}