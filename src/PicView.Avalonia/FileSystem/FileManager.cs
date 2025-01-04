using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC.PopUps;
using PicView.Core.FileHandling;
using PicView.Core.Localization;

namespace PicView.Avalonia.FileSystem;

public static class FileManager
{
    public static async Task DeleteFile(bool recycle, MainViewModel vm)
    {
        if (vm.FileInfo is null)
        {
            return;
        }
        
        var errorMsg = string.Empty;
        
        if(!recycle)
        {
            var prompt = $"{TranslationHelper.GetTranslation("DeleteFilePermanently")}";
            var deleteDialog = new DeleteDialog(prompt, vm.FileInfo.FullName);
            UIHelper.GetMainView.MainGrid.Children.Add(deleteDialog);
        }
        else
        {
            errorMsg = await Task.FromResult(FileDeletionHelper.DeleteFileWithErrorMsg(vm.FileInfo.FullName, recycle));
        }

        if (!string.IsNullOrEmpty(errorMsg))
        {
            await TooltipHelper.ShowTooltipMessageAsync(errorMsg, true);
        }
    }
}
