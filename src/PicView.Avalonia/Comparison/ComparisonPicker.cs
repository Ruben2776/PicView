using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.FileSystem;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.UI;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Comparison;

/// <summary>
/// Picks a file and shows it in A or B of a tab's comparison.
/// </summary>
public static class ComparisonPicker
{
    /// <summary>Picks a file for <paramref name="slot"/> of the active tab's comparison.</summary>
    public static ValueTask PickAsync(MainWindowViewModel vm, MainWindow window, ComparisonSlot slot) =>
        PickAsync(vm.WindowTabs.ActiveTab.CurrentValue, window, slot, FilePicker.SelectFile,
            file => GetImageModel.GetImageModelAsync(file, core: vm.Core));

    /// <summary>
    /// Picks a file with <paramref name="selectFile"/>, loads it with <paramref name="load"/> and shows it in
    /// <paramref name="slot"/>. An image that cannot render shows a tooltip in <paramref name="window"/>, when there is
    /// one. A model that is not shown, because it cannot render or the comparison is gone, is disposed.
    /// </summary>
    internal static async ValueTask PickAsync(TabViewModel tab, MainWindow? window, ComparisonSlot slot,
        Func<Task<string?>> selectFile, Func<FileInfo, ValueTask<ImageModel>> load)
    {
        if (tab.Comparison is not { } comparison)
        {
            return;
        }

        var path = await selectFile().ConfigureAwait(false);
        if (path is null)
        {
            return;
        }

        using var busy = tab.ClaimBusy();
        var model = await load(new FileInfo(path)).ConfigureAwait(false);
        if (model.Image is null)
        {
            model.Dispose();
            if (window is not null)
            {
                await TooltipHelper.ShowTooltipMessageAsync(
                    TranslationManager.Translation.UnableToRender ?? "Unable to load image", true, window,
                    TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            }

            return;
        }

        // The comparison may have been dropped, or replaced, while the file loaded
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (ReferenceEquals(tab.Comparison, comparison) && !comparison.IsDisposed)
            {
                comparison.SetOverride(slot, model);
            }
            else
            {
                model.Dispose();
            }
        });
    }
}
