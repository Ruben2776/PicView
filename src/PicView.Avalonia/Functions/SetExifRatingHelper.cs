using PicView.Core.Exif;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Functions;

public static class SetExifRatingHelper
{
    private static bool CanProceed(MainWindowViewModel vm)
    {
        return vm?.WindowTabs?.ActiveTab?.CurrentValue?.FileInfo?.CurrentValue is not null;
    }
    
    private static void SetValue(ExifViewModel exif, uint value)
    {
        exif?.ExifRating?.Value = value;
    }

    public static async Task Set0Star(MainWindowViewModel vm)
    {
        if (!CanProceed(vm))
        {
            return;
        }
        
        await ExifWriter.SetExifRatingAsync(vm.WindowTabs.ActiveTab.CurrentValue.FileInfo.CurrentValue, 0).ConfigureAwait(false);
        SetValue(vm.Exif, 0);
    }

    public static async Task Set1Star(MainWindowViewModel vm)
    {
        if (!CanProceed(vm))
        {
            return;
        }

        await ExifWriter.SetExifRatingAsync(vm.WindowTabs.ActiveTab.CurrentValue.FileInfo.CurrentValue, 1).ConfigureAwait(false);
        SetValue(vm.Exif, 1);
    }

    public static async Task Set2Star(MainWindowViewModel vm)
    {
        if (!CanProceed(vm))
        {
            return;
        }

        await ExifWriter.SetExifRatingAsync(vm.WindowTabs.ActiveTab.CurrentValue.FileInfo.CurrentValue, 2).ConfigureAwait(false);
        SetValue(vm.Exif, 2);
    }

    public static async Task Set3Star(MainWindowViewModel vm)
    {
        if (!CanProceed(vm))
        {
            return;
        }

        await ExifWriter.SetExifRatingAsync(vm.WindowTabs.ActiveTab.CurrentValue.FileInfo.CurrentValue, 3).ConfigureAwait(false);
        SetValue(vm.Exif, 3);
    }

    public static async Task Set4Star(MainWindowViewModel vm)
    {
        if (!CanProceed(vm))
        {
            return;
        }

        await ExifWriter.SetExifRatingAsync(vm.WindowTabs.ActiveTab.CurrentValue.FileInfo.CurrentValue, 4).ConfigureAwait(false);
        SetValue(vm.Exif, 4);
    }

    public static async Task Set5Star(MainWindowViewModel vm)
    {
        if (!CanProceed(vm))
        {
            return;
        }

        await ExifWriter.SetExifRatingAsync(vm.WindowTabs.ActiveTab.CurrentValue.FileInfo.CurrentValue, 5).ConfigureAwait(false);
        SetValue(vm.Exif, 5);
    }
}