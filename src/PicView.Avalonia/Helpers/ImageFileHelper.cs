using PicView.Core.ImageDecoding;

namespace PicView.Avalonia.Helpers;

internal static class ImageFileHelper
{
    internal static async Task ResizeImageByPercentage(FileInfo fileInfo, int selectedIndex)
    {
        var percentage = 100 - selectedIndex * 5;

        if (percentage is < 5 or > 100)
        {
            return;
        }

        await SaveImageFileHelper.ResizeImageByPercentage(fileInfo, percentage);
    }
}