using PicView.Core.DebugTools;
using PicView.Core.ViewModels;

namespace PicView.Core.Conversion;

public static class ConversionHelper
{
    public static bool DetermineIfOptimizeImageShouldBeEnabled(FileInfo? fileInfo)
    {
        if (fileInfo is null || !fileInfo.Exists)
        {
            return false;
        }

        try
        {
            var extension = Path.GetExtension(fileInfo.FullName.AsSpan());
            return extension.Equals(".jpg", StringComparison.InvariantCultureIgnoreCase)
                   || extension.Equals(".jpeg", StringComparison.InvariantCultureIgnoreCase)
                   || extension.Equals(".png", StringComparison.InvariantCultureIgnoreCase)
                   || extension.Equals(".gif", StringComparison.InvariantCultureIgnoreCase);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(ConversionHelper), nameof(DetermineIfOptimizeImageShouldBeEnabled), e);
            return false;
        }
    }
    
    /// <summary>
    /// Whether the tab can print: it shows an image and the window it is in does not show images side by side.
    /// </summary>
    public static bool DetermineIfPrintMenuItemShouldBeEnabled(TabViewModel tab) =>
        tab.Image.CurrentValue is not null &&
        !(tab.CurrentWindowSettings.CurrentValue?.IsSideBySide.CurrentValue ??
          WindowSettings.IsSideBySideWithoutWindow(Settings));
}