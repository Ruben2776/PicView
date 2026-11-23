using PicView.Core.Conversion;
using PicView.Core.Saving;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.ImageHandling;

/// <summary>
/// Provides image optimization functionality
/// </summary>
public static class ImageOptimizer
{
    /// <summary>
    /// Optimizes the current image in the view model losslessly, through the save module
    /// </summary>
    /// <param name="vm">The main view model</param>
    public static async Task OptimizeImageAsync(MainWindowViewModel vm)
    {
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        if (!ConversionHelper.DetermineIfOptimizeImageShouldBeEnabled(tab.FileInfo?.CurrentValue))
        {
            return;
        }

        await vm.Core.Writer.WriteAsync(tab,
            new ShownFileWrite(WriteSource.Unedited, new WriteEdits(LosslessOptimize: true),
                new WriteDestination.OverItself())).ConfigureAwait(false);
    }
}
