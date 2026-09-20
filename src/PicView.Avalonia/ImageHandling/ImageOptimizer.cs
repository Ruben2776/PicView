using PicView.Core.Conversion;
using PicView.Core.DebugTools;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.ImageHandling;

/// <summary>
/// Provides image optimization functionality
/// </summary>
public static class ImageOptimizer
{
    /// <summary>
    /// Optimizes the current image in the view model
    /// </summary>
    /// <param name="vm">The main view model</param>
    public static async Task OptimizeImageAsync(MainWindowViewModel vm)
    {
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        if (ConversionHelper.DetermineIfOptimizeImageShouldBeEnabled(tab.FileInfo?.CurrentValue))
        {
            return;
        }

        vm.IsLoadingIndicatorShown.Value = true;
        await Task.Run(() =>
        {
            var file = tab.FileInfo.CurrentValue;
            try
            {
                var optimizer = new ImageMagick.ImageOptimizer
                {
                    OptimalCompression = true
                };
                optimizer.LosslessCompress(file.FullName);
            }
            catch (Exception ex)
            {
                DebugHelper.LogDebug(nameof(ImageOptimizer), nameof(OptimizeImageAsync), ex);
            }
        }).ConfigureAwait(false);
        tab.ImageIterator.Cache.DeleteFromCache(tab.FileInfo.CurrentValue.FullName);
        await tab.ImageIterator.ReloadAsync(clearCache:false).ConfigureAwait(false);
        vm.IsLoadingIndicatorShown.Value = false;
        
        // TODO: have to create a new loading view, that blocks navigation until finished. With a cancel button.
    }
}