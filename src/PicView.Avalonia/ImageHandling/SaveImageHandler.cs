using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;

namespace PicView.Avalonia.ImageHandling;

public static class SaveImageHandler
{
    public static async Task SaveImageWithPossibleNavigation(MainViewModel vm,
        string path,
        string destination,
        bool sameFile,
        string? ext = null,
        uint? width = null,
        uint? height = null,
        uint? quality = null,
        uint? rotationAngle = null,
        bool isKeepingAspectRatio = true)
    {
        var success = await SaveImageFileHelper.SaveImageAsync(
            null, path, destination, width, height, quality,
            ext, rotationAngle, null, isKeepingAspectRatio).ConfigureAwait(false);

        if (!success)
        {
            TooltipHelper.ShowTooltipMessage(TranslationManager.Translation.SavingFileFailed);
            return;
        }
        
        if (Path.GetExtension(path) != ext && sameFile)
        {
            // Delete the old file
            await vm.PlatformService.DeleteFile(path, true); 
        }

        // Clear possible cache to show updated values correctly
        NavigationManager.RemoveFromPreloader(path);
        NavigationManager.RemoveFromPreloader(destination);

        if (sameFile)
        {
            await NavigationManager.LoadPicFromFile(destination, vm).ConfigureAwait(false);
        }
    }
}