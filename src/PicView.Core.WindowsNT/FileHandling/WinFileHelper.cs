using Microsoft.VisualBasic.FileIO;
using PicView.Core.DebugTools;

namespace PicView.Core.WindowsNT.FileHandling;

public static class WinFileHelper
{
    public static bool DeleteFile(string filePath, bool moveToRecycleBin)
    {
        try
        {
            var recycleOption = moveToRecycleBin ? RecycleOption.SendToRecycleBin : RecycleOption.DeletePermanently;
            FileSystem.DeleteFile(filePath, UIOption.AllDialogs, recycleOption);
            return File.Exists(filePath);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(WinFileHelper), nameof(DeleteFile), e);
            return false;
        }
    }
}
